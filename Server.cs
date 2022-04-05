using Extensions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tcpServer.Entity;

namespace tcpServer
{
    class Server
    {
        private int maxConnections;
        private static byte[] emptyArray = Array.Empty<byte>();
        private int receivebuffSize;
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
        int m_totalBytesRead;           // counter of the total # bytes received by the server
        int m_numConnectedSockets;      // the total number of clients connected to the server
        public Server(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            Array.Resize<byte>(ref emptyArray, receiveBufferSize);
            m_numConnectedSockets = 0;
            maxConnections = numConnections;
            Array.Resize<Player>(ref Player.Players, maxConnections);
            receivebuffSize = receiveBufferSize;
        }

        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(maxConnections);

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
            m_numConnectedSockets++;
            OnAccept(e);
        }
        // This method is called whenever a receive or send operation is completed on a socket
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
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
            //ReadWrite.AcceptSocket = e.AcceptSocket;

            ReadWrite.UserToken = new Player(e.AcceptSocket) { ConnectedId = m_numConnectedSockets - 1 };
            Player.Players[m_numConnectedSockets - 1] = ReadWrite.UserToken as Player;
            if(m_numConnectedSockets > 1)
            {
                ReadWrite.SetBuffer(new byte[] { 0, (byte)m_numConnectedSockets },0,2);
                foreach (Player p in Player.Players) if (p is not null) (ReadWrite.UserToken as Player).Sock.SendAsync(ReadWrite);
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
                //increment the count of the total bytes receive by the server
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);
                //echo the data received back to the client
                e.SetBuffer(e.Offset, e.BytesTransferred);
                if (e.Buffer[0] == 1)
                {
                    Packet pack = new(e.Buffer);
                    (e.UserToken as Player).Position = pack.ReadDataAfterHeader().ToVec2();
                    Console.WriteLine($"Player with id {(e.UserToken as Player).ConnectedId} pos X:{(e.UserToken as Player).Position.X} Y: {(e.UserToken as Player).Position.Y}");
                    if (m_numConnectedSockets > 1)
                    {
                        byte[] positionspack = emptyArray;
                        Array.Resize<byte>(ref positionspack, (m_numConnectedSockets - 1) * 4);
                        int skipped = 0;
                        for (int i = 0; i < m_numConnectedSockets; i++)
                        {
                            if (Player.Players[i] is null) continue;
                            if (Player.Players[i].ConnectedId == ((Player)e.UserToken).ConnectedId) { skipped++; continue; }
                            Array.Copy(Player.Players[i].PositionAsByte(), 0, positionspack, (i - skipped) * 4, 4);
                        }
                        pack.SetData(positionspack);
                        pack.SetHeader(PacketId.Position);
                        e.SetBuffer(pack.ReadData(), 0, pack.Count);
                        foreach (Player p in Player.Players)
                        {
                            if (p is null) continue;
                            if (p.ConnectedId == ((Player)e.UserToken).ConnectedId) continue;
                            p.Sock.Send(e.Buffer);
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
            m_numConnectedSockets--;
            /*if (Player.Players[m_numConnectedSockets + 1] is not null)
            {
                Player.Players[m_numConnectedSockets] = Player.Players[m_numConnectedSockets + 1];
            }
            else
            {
                Player.Players[m_numConnectedSockets] = null;
            }*/
                if (m_numConnectedSockets > 1)
            {
                foreach (Player p in Player.Players)if( p is not null)p.Sock.Send(new byte[] { 0, (byte)m_numConnectedSockets });
            }
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }
        private async ValueTask<bool> Read(SocketAsyncEventArgs e, int numBytes)
        {
            int bytesRead = 0;

            if (numBytes > 1024) return false;

            try
            {
                while (bytesRead < numBytes)
                {

                    var buff = new Memory<byte>(e.Buffer, bytesRead, numBytes - bytesRead);
                    bytesRead = await ((Socket)(e.UserToken)).ReceiveAsync(buff, 0);

                    if (bytesRead == 0) return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}