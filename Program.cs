using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace tcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //    StartServer();
            Server server = new(10, 256);
            server.Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2050));
            Console.ReadKey();
        }
        /*static async void StartServer()
        {
            Socket server = new(SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2050));
            server.Listen(100);
            while (true)
            {
                Console.WriteLine("Waiting for connection");
                HandleConnection(await server.AcceptAsync()); // when the await is done, this method is called and thread is changed to one of the threads in taskfactory threadpool (to keep the main thread running)
            }
        }
        static async void HandleConnection(Socket soc)
        {
            Console.WriteLine("Client connected.");
            var buffer = ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                while (soc.Connected)// --> until here when the context is switched back to the caller (the while loop, HERE!)
                {
                    int count = await soc.ReceiveAsync(buffer, 0);
                    string str = Encoding.UTF8.GetString(buffer.AsSpan()[..count]);// after the await is done again the thread is changed to an available thread in the threadpool
                    await soc.SendAsync(Encoding.UTF8.GetBytes(str.ToUpper()), 0);
                    Console.WriteLine(str);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch (SocketException e)
            {
                soc.Close();
                if (e.NativeErrorCode != 10054)
                    Console.WriteLine(e);
            }
        }*/

    }
}
