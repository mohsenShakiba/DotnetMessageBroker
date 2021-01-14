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

        public bool Match(Span<byte> messageRoute, Span<byte> subscriberRoute)
        {
            var maxLength = Math.Max(messageRoute.Length, subscriberRoute.Length);

            var messageRouteIndex = 0;
            var subscriberRouteIndex = 0;
            var match = false;

            for(var i = 0; i < maxLength; i++)
            {
                if (i == messageRouteIndex || i == subscriberRouteIndex)
                {
                    break;
                }

                var subscriberRouteChar = subscriberRoute[subscriberRouteIndex];
                var messageRouteChar = messageRoute[messageRouteIndex];

                if (subscriberRouteChar == messageRouteChar)
                {
                    messageRouteIndex += 1;
                    subscriberRouteIndex += 1;
                    continue;
                }

                if (subscriberRouteChar == '/' && messageRouteChar == '/')
                {
                    messageRouteIndex += 1;
                    subscriberRouteIndex += 1;
                    continue;
                }

                if (subscriberRouteChar == '*')
                {
                    var indexOfNexDelimiter = messageRoute.Slice(messageRouteIndex).IndexOf(Delimiter);
                    subscriberRouteIndex += 1;
                    messageRouteIndex += indexOfNexDelimiter;
                }
            }
        }
    }
}
