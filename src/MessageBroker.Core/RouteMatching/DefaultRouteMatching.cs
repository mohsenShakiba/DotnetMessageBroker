using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core.RouteMatching
{
    public class DefaultRouteMatching : IRouteMatcher
    {
        private static byte[] _delimiter;

        public static Span<byte> Delimiter
        {
            get
            {
                if (_delimiter != null)
                {
                    return _delimiter.AsSpan();
                }

                var delimiter = "\n";
                var delimiterB = Encoding.ASCII.GetBytes(delimiter);
                _delimiter = delimiterB;
                return delimiterB;
            }
        }

        public bool Match(string messageRoute, string subscriberRoute)
        {
            return messageRoute == subscriberRoute;
        }

       
    }
}
