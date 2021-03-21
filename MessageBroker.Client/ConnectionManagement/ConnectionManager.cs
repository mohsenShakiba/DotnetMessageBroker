using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Exceptions;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;

namespace MessageBroker.Client.ConnectionManagement
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly Guid Id;
        private readonly IReceiveDataProcessor _receiveDataProcessor;
        private readonly IBinaryDataProcessor _binaryDataProcessor;

        private IClientSession _clientSession;
        private ITcpSocket _tcpSocket;
        private IPEndPoint _endPoint;
        private bool _ready;
        private int _test;

        public event Action OnConnected;
        public event Action OnDisconnected;

        public IClientSession ClientSession => _clientSession;
        public bool Connected => _tcpSocket.Connected;


        public ConnectionManager(IReceiveDataProcessor receiveDataProcessor, IBinaryDataProcessor binaryDataProcessor)
        {
            _receiveDataProcessor = receiveDataProcessor;
            _binaryDataProcessor = binaryDataProcessor;
            SetDefaultTcpSocket();
            Id = Guid.NewGuid();
        }

        private void SetDefaultTcpSocket()
        {
            _tcpSocket = new TcpSocket();

            _receiveDataProcessor.OnReadyReceived += MarkAsReady;
        }

        public void SetAlternativeTcpSocketForTesting(ITcpSocket tcpSocket)
        {
            _tcpSocket = tcpSocket;
        }

        public void Connect(IPEndPoint ipEndPoint)
        {
            _ = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));

            _endPoint = ipEndPoint;

            _tcpSocket.Connect(ipEndPoint);
            
            _clientSession = new ClientSession(new BinaryDataProcessor());

            _clientSession.ForwardEventsTo(this);
            _clientSession.ForwardDataTo(_receiveDataProcessor);
            _clientSession.Use(_tcpSocket);

            _test = 0;

            ClientConnected(_clientSession);
        }

        public void Reconnect()
        {
            Connect(_endPoint);
        }

        public void Disconnect()
        {
            _tcpSocket.Disconnect(true);
            _clientSession.Close();
        }

        public void SimulateInterrupt()
        {
            _tcpSocket.Disconnect(true);
        }

        public async ValueTask WaitForReadyAsync(CancellationToken cancellationToken)
        {
            if (_ready)
            {
                return;
            }

            if (!Connected)
            {
                throw new SocketNotConnectedException();
            }

            
            while (!cancellationToken.IsCancellationRequested && !_ready)
            {
                Logger.LogInformation($"waiting for ready signal for id {_clientSession.Id} {_ready} {_test} {Id}");
                
                await Task.Delay(10, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                throw new TaskCanceledException();
            
        }

        public void MarkAsReady()
        {
            _ready = true;
            _test = 1;
            Logger.LogInformation($"ready received for {_clientSession.Id} {_ready} {_test} {Id}");
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            Logger.LogInformation($"Client disconnected {_clientSession.Id} {clientSession.Id} {Id}");
            _ready = false;
            _test = -1;
            OnDisconnected?.Invoke();
        }

        public void ClientConnected(IClientSession clientSession)
        {
            Logger.LogInformation($"Client connected {_clientSession.Id}");
            OnConnected?.Invoke();
        }
    }
}