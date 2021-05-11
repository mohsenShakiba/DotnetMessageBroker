// using System;
// using MessageBroker.TCP;
// using MessageBroker.TCP.Client;
//
// namespace Tests.Classes
// {
//     public class TestSocketEventProcessor : ISocketEventProcessor, ISocketDataProcessor
//     {
//         public void DataReceived(Guid sessionId, Memory<byte> payload)
//         {
//             OnDataReceived?.Invoke(sessionId, payload);
//         }
//
//         public void ClientConnected(IClient client)
//         {
//             OnClientConnected?.Invoke(client.Id);
//         }
//
//         public void ClientDisconnected(IClient client)
//         {
//             OnClientDisconnected?.Invoke(client.Id);
//         }
//
//         public event Action<Guid, Memory<byte>> OnDataReceived;
//         public event Action<Guid> OnClientConnected;
//         public event Action<Guid> OnClientDisconnected;
//     }
// }