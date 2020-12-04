using System;

namespace MessageBroker.SocketServer.Models
{
    public record MessagePayload(ReadOnlyMemory<byte> Date, Guid sessionId);
}