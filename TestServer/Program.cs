using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;

namespace TcpChatClient
{
    class ChatClient : TcpClient
    {
        public ChatClient(string address, int port) : base(address, port) { }

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat TCP client connected a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            Console.WriteLine(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP client caught an error with code {error}");
        }

        private bool _stop;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // TCP server address
            string address = "127.0.0.1";
            if (args.Length > 0)
                address = args[0];

            // TCP server port
            int port = 8080;
            if (args.Length > 1)
                port = int.Parse(args[1]);

            Console.WriteLine($"TCP server address: {address}");
            Console.WriteLine($"TCP server port: {port}");

            Console.WriteLine();

            // Create a new TCP chat client
            //var client = new ChatClient();
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            // Connect the client
            Console.Write("Client connecting...");
            client.Connect(address, port);
            Console.WriteLine("Done!");

            Thread.Sleep(1000);

            Console.WriteLine("Press Enter to stop the client or '!' to reconnect the client...");
            client.SendBufferSize = 10024;

            // Perform text input
            for (; ; )
            {
                //string line = Console.ReadLine();
                //if (string.IsNullOrEmpty(line))
                //    break;

                // Disconnect the client
                //if (line == "!")
                //{
                //    Console.Write("Client disconnecting...");
                //    client.DisconnectAsync();
                //    Console.WriteLine("Done!");
                //    continue;
                //}

                // Send the entered text to the chat server
                var msg = "this is a test";
                var length = msg.Length;
                var lengthBinary = BitConverter.GetBytes(length);
                var msgBinary = Encoding.UTF8.GetBytes(msg);
                var z = new byte[lengthBinary.Length + msgBinary.Length];
                lengthBinary.CopyTo(z, 0);
                msgBinary.CopyTo(z, lengthBinary.Length);
                client.Send(z);
                //Thread.Sleep(100);

            }

            // Disconnect the client
            Console.Write("Client disconnecting...");
            client.Disconnect(true);
            Console.WriteLine("Done!");
        }
    }
}