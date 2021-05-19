using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Async;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Tcp;
using MessageBroker.Common.Tcp.EventArgs;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Topics;

namespace MessageBroker.Core.Clients
{
    /// <summary>
    /// A wrapper around TcpSocket which helps with the process of receiving data
    /// the process of receiving data is a bit complicated and requires the usage of IBinaryDataProcessor
    /// Server will read data in chunk from socket and write it to BinaryDataProcessor
    /// once a payload is received completely the data is dispatched to OnDataReceived event
    /// </summary>
    public interface IClient : IDisposable
    {
        /// <summary>
        /// Identifier of this IClientSession
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The max number of parallel messages that can be sent to client
        /// </summary>
        int MaxConcurrency { get; }

        /// <summary>
        /// Returns true if <see cref="Close" /> is called or object is disposed
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Used by the <see cref="ITopic" /> to check if client is available
        /// </summary>
        bool ReachedMaxConcurrency { get; }

        /// <summary>
        /// Event for when client disconnects
        /// </summary>
        event EventHandler<ClientSessionDisconnectedEventArgs> OnDisconnected;

        /// <summary>
        /// Event for when client payload is received in whole
        /// </summary>
        event EventHandler<ClientSessionDataReceivedEventArgs> OnDataReceived;

        /// <summary>
        /// Will setup the client to use <see cref="ISocket" /> as the underlying socket object
        /// </summary>
        /// <param name="socket">Provided socket to use as underlying socket</param>
        /// <exception cref="InvalidOperationException">If the provided socket is not in connected state</exception>
        void Setup(ISocket socket);

        /// <summary>
        /// Will start a dedicated thread for receiving data from <see cref="ISocket" />
        /// </summary>
        void StartReceiveProcess();

        /// <summary>
        /// Will start a dedicated thread for sending data that is added by <see cref="Enqueue" />
        /// </summary>
        void StartSendProcess();

        /// <summary>
        /// Will read the next message from queue and send it to <see cref="ISocket" />
        /// </summary>
        /// <returns>Task for when sending is complete</returns>
        /// <remarks>Use for testing only</remarks>
        Task SendNextMessageInQueue();

        /// <summary>
        /// Will send payload data to client immediately
        /// </summary>
        /// <param name="payload">The memory to send to client</param>
        /// <param name="cancellationToken">Token for cancellation</param>
        /// <returns>True if sent was successful</returns>
        /// <remarks>If return value is false then the client will be automatically closed</remarks>
        Task<bool> SendAsync(Memory<byte> payload, CancellationToken cancellationToken);

        /// <summary>
        /// Will send the <see cref="SerializedPayload" /> to client and return a <see cref="AsyncPayloadTicket" />
        /// </summary>
        /// <param name="serializedPayload">Payload to be sent</param>
        /// <returns>Ticket for tracking the payload status</returns>
        /// <remarks>
        /// Before calling send <see cref="ReachedMaxConcurrency" /> should be called to make sure
        /// the client won't be flooded with too many messages
        /// </remarks>
        AsyncPayloadTicket Enqueue(SerializedPayload serializedPayload);

        /// <summary>
        /// Same as <see cref="Enqueue" /> but will not create a <see cref="AsyncPayloadTicket" />
        /// </summary>
        /// <param name="serializedPayload"></param>
        void EnqueueFireAndForget(SerializedPayload serializedPayload);

        /// <summary>
        /// Called by the <see cref="IPayloadProcessor" /> when ack is received by the client
        /// </summary>
        /// <param name="payloadId">Identifier of the payload</param>
        void OnPayloadAckReceived(Guid payloadId);

        /// <summary>
        /// Called by the <see cref="IPayloadProcessor" /> when nack is received by the client
        /// </summary>
        /// <param name="payloadId">Identifier of the payload</param>
        void OnPayloadNackReceived(Guid payloadId);

        /// <summary>
        /// Max number of concurrency, this number is used to control the number of concurrent messages
        /// sent to client at each time
        /// </summary>
        /// <param name="maxConcurrency">Max number of concurrency</param>
        void ConfigureConcurrency(int maxConcurrency);

        /// <summary>
        /// Will try to disconnect the TcpSocket and dispose the
        /// </summary>
        void Close();
    }
}