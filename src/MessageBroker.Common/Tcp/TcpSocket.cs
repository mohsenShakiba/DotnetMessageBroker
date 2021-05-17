using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.TCP
{
    /// <inheritdoc cref="ISocket" />
    public sealed class TcpSocket : ISocket
    {
        private readonly Socket _socket;

        private bool _disposed;
        
        public bool Connected => _socket?.Connected ?? false;

        public TcpSocket(Socket socket)
        {
            _socket = socket;
        }

        public static TcpSocket NewFromEndPoint(IPEndPoint ipEndPoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipEndPoint);
            return new TcpSocket(socket);
        }

        public void Disconnect()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            catch (SocketException)
            {
                // ignore SocketException
            }
            catch (ObjectDisposedException)
            {
                // ignore ObjectDisposedException
            }

            // dispose the object
            Dispose();
        }

        public void SimulateInterrupt()
        {
            Disconnect();
        }

        public async ValueTask<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken)
        {
            if (_disposed)
                return 0;
            
            try
            {
                return await _socket.SendAsync(data, SocketFlags.None, cancellationToken: cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return 0;
            }
            catch
            {
                Disconnect();
                return 0;
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_disposed)
                return 0;
            
            try
            {
                return await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return 0;
            }
            catch
            {
                Disconnect();
                return 0;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _socket.Dispose();
                    _disposed = true;
                }
                catch (ObjectDisposedException)
                {
                    // ignore ObjectDisposedException
                }
            }
        }

    }
}