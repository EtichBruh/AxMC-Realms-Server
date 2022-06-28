using System;
using System.Buffers;
using System.Net;
using System.Net.Http;
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
            new HttpClient().GetAsync("https://sus.7hemech.repl.co/reset");
        }
    }
}
