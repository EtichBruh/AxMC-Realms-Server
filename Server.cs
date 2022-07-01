using Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tcpServer.Entity;

namespace tcpServer
{
    class Server
    {
        private int maxconnects;
        private static byte[] emptyArray = Array.Empty<byte>();
        private int receivebuffSize;
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
        int m_totalBytesRead;           // counter of the total # bytes received by the server
        int numconnects;      // the total number of clients connected to the server
        public Server(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            Array.Resize<byte>(ref emptyArray, receiveBufferSize);
            numconnects = 0;
            maxconnects = numConnections;
            Array.Resize<Player>(ref Player.Players, maxconnects);
            receivebuffSize = receiveBufferSize;
        }

        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(maxconnects);

            // post accepts on the listening socket
            StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += AcceptEventArg_Completed;
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }
            //bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg); 
            if (!listenSocket.AcceptAsync(acceptEventArg))
            {
                OnAccept(acceptEventArg);
            }
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("Client Connected");
            new HttpClient().GetAsync("https://sus.7hemech.repl.co/join");
            numconnects++;
            OnAccept(e);
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    OnReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    OnSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        void OnAccept(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgs ReadWrite = new();

            ReadWrite.UserToken = new Player(e.AcceptSocket) { ConnectedId = numconnects - 1 };
            Player.Players[numconnects - 1] = ReadWrite.UserToken as Player;
            if (numconnects > 1)
            {
                ReadWrite.SetBuffer(new byte[] { 0, (byte)numconnects });
                foreach (Player p in Player.Players) if (p is not null) p.Sock.SendAsync(ReadWrite);
            }
            ReadWrite.SetBuffer(emptyArray, 0, emptyArray.Length);
            ReadWrite.Completed += IO_Completed;
            if (!e.AcceptSocket.ReceiveAsync(ReadWrite))
            {
                OnReceive(ReadWrite);
            }
            StartAccept(e);
        }
        void OnReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Console.WriteLine("The server has read {0} bytes", e.BytesTransferred);
                //process data
                e.SetBuffer(e.Offset, e.BytesTransferred);
                int pid = (e.UserToken as Player).ConnectedId;
                if(e.BytesTransferred > 5)
                {
                    Console.WriteLine(e.Buffer.ToString());
                }
                if (e.Buffer[0] == 1)
                {
                    Packet pack = new(e.Buffer);
                    (e.UserToken as Player).Position = pack.ReadAfterHeader().ToVec2();
                    Console.WriteLine($"Player id {pid} pos X:{(e.UserToken as Player).Position.X} Y: {(e.UserToken as Player).Position.Y}");
                    if (numconnects > 1)
                    {
                        byte[] positionspack = new byte[0];// = new byte[(m_numConnectedSockets - 1) * 4];
                        List<byte> poses = new();
                        int skipped = 0;
                        for (int i = 0; i < numconnects; i++)
                        {
                            if (Player.Players[i] is null) continue;
                            //if (Player.Players[i].ConnectedId == pid) { continue; }
                            for(int j = 0; j < numconnects; j++)
                            {
                                if (Player.Players[i].ConnectedId != Player.Players[j].ConnectedId)
                                {
                                    poses.AddRange(Player.Players[i].PositionByte());
                                }
                                positionspack = poses.ToArray();
                                pack.SetData(positionspack);
                                pack.SetHeader(PacketId.Position);
                                Player.Players[j].Sock.Send(pack.ReadData());
                                poses.Clear();
                            }
                            //Array.Copy(Player.Players[i].PositionByte(), 0, positionspack, (i - skipped) * 4, 4);
                        }
                        e.SetBuffer(positionspack, 0, positionspack.Length);
                        foreach (Player p in Player.Players)
                        {
                            if (p is null) continue;
                            if (p.ConnectedId == pid)
                                continue;
                            p.Sock.Send(pack.ReadData());
                            Console.WriteLine($"Server sent pos {pid} to {p.ConnectedId}");
                        }
                    }
                }
                if (!(e.UserToken as Player).Sock.SendAsync(e))
                {
                    OnSend(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        void OnSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                e.SetBuffer(emptyArray, 0, emptyArray.Length);
                if (!(e.UserToken as Player).Sock.ReceiveAsync(e))
                {
                    OnReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            // close the socket associated with the client
            try
            {
                (e.UserToken as Player).Sock.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            (e.UserToken as Player).Sock.Close();
            e.Dispose();
            numconnects--;

            if (numconnects >= 0)
            {
                foreach (Player p in Player.Players) if (p is not null) p.Sock.Send(new byte[] { 0, (byte)numconnects });
            }
            new HttpClient().GetAsync("https://sus.7hemech.repl.co/leave");

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", numconnects);
        }
    }
}