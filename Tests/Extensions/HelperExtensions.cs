using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Extensions
{
    public static class HelperExtensions
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

        public static void AddWithDelimiter(this List<byte> b, byte[] d)
        {
            var delimiter = Encoding.UTF8.GetBytes("\n");
            b.AddRange(d);
            b.AddRange(delimiter);
        }
    }
}
