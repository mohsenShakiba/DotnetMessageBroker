namespace MessageBroker.Serialization
{
    public static class SerializationConfig
    {
        /// <summary>
        ///     the size that send payloads buffer starts at
        ///     this size might increase over time as the payload size exceed this amount
        /// </summary>
        public const int SendPayloadStartingBufferSize = 128;

        /// <summary>
        ///     the size that receive buffer starts at
        ///     this size might increase over time as the payload size exceed this amount
        /// </summary>
        public const int ReceivePayloadStartingBufferSize = 128;
    }
}