using System;
using System.Linq;
using System.Text;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;

namespace Tests.Classes
{
    public static class RandomGenerator
    {
        public static string GenerateString(int length, Random random = null)
        {
            random ??= new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static byte[] GenerateBytes(int length, Random random = null)
        {
            return Encoding.UTF8.GetBytes(GenerateString(length, random));
        }

        public static SerializedPayload SerializedPayload(int dataSize = 10, PayloadType type = PayloadType.Msg)
        {
            var sp = ObjectPool.Shared.Rent<SerializedPayload>();
            sp.FillFrom(GenerateBytes(dataSize), dataSize, Guid.NewGuid(), type);
            return sp;
        }
    }
}