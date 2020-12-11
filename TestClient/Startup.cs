using MessageBroker.SocketServer.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;

namespace TcpChatServer
{

    class Program
    {
        static void Main(string[] args)
        {
            // TCP server port
            int port = 8080;
            if (args.Length > 0)
                port = int.Parse(args[0]);

            Console.WriteLine($"TCP server port: {port}");

            Console.WriteLine();

            // Create a new TCP chat server
            var endpoint = new IPEndPoint(IPAddress.Loopback, port);
            var server = new TcpSocketServer(endpoint, null);

            // Start the server
            Console.Write("Server starting... press ! to stop");
            server.Start();

            // Perform text input
            for (; ; )
            {
                string line = Console.ReadLine();

                // Restart the server
                if (line == "!")
                {
                    server.Stop();
                    break;
                }

            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }
    }
}