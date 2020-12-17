using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core.Extensions
{
    public static class BinaryExtensions
    {

        public static void AddWithDelimiter(this List<byte> b, string s)
        {
            var delimiter = Encoding.UTF8.GetBytes("\n");
            b.AddRange(Encoding.UTF8.GetBytes(s));
            b.AddRange(delimiter);
        }

        public static void AddWithDelimiter(this List<byte> b, Guid g)
        {
            var delimiter = Encoding.UTF8.GetBytes("\n");
            b.AddRange(g.ToByteArray());
            b.AddRange(delimiter);
        }

        public static void AddWithDelimiter(this List<byte> b, int i)
        {
            var delimiter = Encoding.UTF8.GetBytes("\n");
            b.AddRange(BitConverter.GetBytes(i));
            b.AddRange(delimiter);
        }

        public static void AddWithDelimiter(this List<byte> b, byte[] d)
        {
            var delimiter = Encoding.UTF8.GetBytes("\n");
            b.AddRange(d);
            b.AddRange(delimiter);
        }
    }
}
