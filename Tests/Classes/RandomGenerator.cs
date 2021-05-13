using System;
using System.Linq;
using System.Text;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.TCP.Binary;

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

        public static SerializedPayload SerializedPayload(PayloadType type = PayloadType.Msg, int dataSize = 10)
        {
            var sp = ObjectPool.Shared.Rent<SerializedPayload>();
            sp.FillFrom(GenerateBytes(dataSize), dataSize, Guid.NewGuid());
            return sp;
        }

        public static double GenerateDouble()
        {
            var random = new Random();
            return random.NextDouble();
        }
    }
}