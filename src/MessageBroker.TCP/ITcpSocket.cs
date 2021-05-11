using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.TCP
{
    /// <summary>
    /// A wrapper interface so that mocking socket behaviour would be easier
    /// the default implementation will accept aa actual TCP socket
    /// </summary>
    public interface ITcpSocket: IDisposable
    {
        /// <summary>
        /// Returns the current connection status of underlying socket
        /// </summary>
        bool Connected { get; }
        
        /// <summary>
        /// Will close the socket connection and dispose the TcpSocket object
        /// </summary>
        /// <exception cref="ObjectDisposedException">The object has been disposed</exception>
        void Disconnect();

        /// <summary>
        /// Will disconnect the underlying socket without disposing the object used only for testing purposes
        /// </summary>
        void SimulateInterrupt();

        /// <summary>
        /// Send data to endpoint and return the number of sent bytes
        /// </summary>
        /// <remarks>If fails, the return value is 0</remarks>
        /// <param name="data">The memory segment to send to endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ObjectDisposedException">The object has been disposed</exception>
        /// <returns>Number of bytes sent</returns>
        ValueTask<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken);
        
        /// <summary>
        /// Will receive data and return the number of received bytes
        /// </summary>
        /// <remarks>If fails, the return value is 0</remarks>
        /// <param name="buffer">The buffer used to write data to</param>
        /// <exception cref="ObjectDisposedException">The object has been disposed</exception>
        /// <returns>Number of bytes sent</returns>
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}