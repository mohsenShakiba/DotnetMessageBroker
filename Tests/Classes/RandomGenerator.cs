using System;
using System.Linq;
using System.Text;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Models.Binary;
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

        public static Message GetMessage(string route)
        {
            return new Message
            {
                Data = Encoding.UTF8.GetBytes(GenerateString(10)),
                Id = Guid.NewGuid(),
                Route = route ?? GenerateString(10),
            };
        }

        public static SerializedPayload GetMessageSerializedPayload(string route = default)
        {
            var serializer = new Serializer();
            return serializer.Serialize(GetMessage(route));
        }

        public static double GenerateDouble()
        {
            var random = new Random();
            return random.NextDouble();
        }
    }
}