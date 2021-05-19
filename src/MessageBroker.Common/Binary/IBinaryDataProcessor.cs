using System;

namespace MessageBroker.Common.Binary
{
    /// <summary>
    /// Payloads received from wire is written to this object
    /// and once a payload has been received completely then it can be read from using <see cref="TryRead" /> method
    /// </summary>
    /// <summary>
    /// because payloads might be received in chunks and an entire payload might take several reads from wire to be
    /// complete, we need to store them temporary and read them once the entire payload has been received
    /// </summary>
    /// <summary>
    /// this object must be used for a single socket connection, writing chunk of data from multiple socket connections
    /// will result in corrupted data
    /// </summary>
    public interface IBinaryDataProcessor : IDisposable
    {
        /// <summary>
        /// Write chunk of payload data to buffer
        /// </summary>
        /// <param name="chunk">Chunk of payload data</param>
        void Write(Memory<byte> chunk);

        /// <summary>
        /// While this lock is active, calling Dispose will prevent the buffer to be disposed
        /// </summary>
        void BeginLock();

        /// <summary>
        /// Release the lock, now the buffer can be disposed
        /// </summary>
        /// <remarks>If Dispose was previously called, then buffer will be disposed once EndLock is called</remarks>
        void EndLock();

        /// <summary>
        /// Read the entire payload if received
        /// </summary>
        /// <param name="binaryPayload">Payload binary data</param>
        /// <returns>True if any payload has been received completely</returns>
        bool TryRead(out BinaryPayload binaryPayload);
    }
}