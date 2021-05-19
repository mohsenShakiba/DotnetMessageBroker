using MessageBroker.Core.RouteMatching;
using Xunit;

namespace Tests.Core.RouteMatching
{
    public class RouteMatcherTests
    {
        private readonly IRouteMatcher _routeMatcher;

        public RouteMatcherTests()
        {
            _routeMatcher = new RouteMatcher();
        }

        [Fact]
        public void Match_Same_ReturnsTrue()
        {
            var messageRoute = "bar/foo";
            var queueRoute = "bar/foo";

            var match = _routeMatcher.Match(messageRoute, queueRoute);

            Assert.True(match);
        }

        [Fact]
        public void Match_Different_ReturnsFalse()
        {
            var messageRoute = "foo/bar";
            var queueRoute = "bar/foo";

            var match = _routeMatcher.Match(messageRoute, queueRoute);
            Assert.False(match);
        }

        [Fact]
        public void Match_WithWildCard_ReturnsTrue()
        {
            {
                var messageRoute = "bar/*";
                var queueRoute = "bar/foo";

                var match = _routeMatcher.Match(messageRoute, queueRoute);
                Assert.True(match);
            }

            {
                var messageRoute = "*";
                var queueRoute = "bar/foo";

                var match = _routeMatcher.Match(messageRoute, queueRoute);
                Assert.True(match);
            }

            {
                var messageRoute = "bar/*/foo";
                var queueRoute = "bar/foo";

                var match = _routeMatcher.Match(messageRoute, queueRoute);
                Assert.True(match);
            }
        }
    }
}