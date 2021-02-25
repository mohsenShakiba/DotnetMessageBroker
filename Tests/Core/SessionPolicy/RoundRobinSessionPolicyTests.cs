using System;
using MessageBroker.Core.SessionPolicy;
using Xunit;

namespace Tests.Core.SessionPolicy
{
    public class RoundRobinSessionPolicyTests
    {
        [Fact]
        public void MakeSureRoundRobinPolicyDoesNotThrowExceptionWhenNoSessionsAreAvailable()
        {
            var sessionPolicy = new RoundRobinSessionPolicy();
            var result = sessionPolicy.GetNextSession();
            Assert.Null(result);
        }


        [Fact]
        public void MakeSureRoundRobinPolicyReturnsHasSessionCorrectly()
        {
            var sessionPolicy = new RoundRobinSessionPolicy();
            
            Assert.False(sessionPolicy.HasSession());
            
            sessionPolicy.AddSession(Guid.NewGuid());
            
            Assert.True(sessionPolicy.HasSession());
        }
        
        [Fact]
        public void MakeSureRoundRobinPolicySelectsSessionsCorrectly()
        {
            var sessionPolicy = new RoundRobinSessionPolicy();

            var sessionList = new [] {Guid.NewGuid(), Guid.NewGuid()}; 
            
            sessionPolicy.AddSession(sessionList[0]);
            sessionPolicy.AddSession(sessionList[1]);

            var session1 = sessionPolicy.GetNextSession();
            var session2 = sessionPolicy.GetNextSession();
            var session3 = sessionPolicy.GetNextSession();
            
            Assert.Equal(sessionList[0], session1);
            Assert.Equal(sessionList[1], session2);
            Assert.Equal(sessionList[0], session3);
        }

        [Fact]
        public void MakeSureIfDuplicateSessionIsAddedThenAnExceptionIsThrown()
        {
            var sessionPolicy = new RoundRobinSessionPolicy();

            var duplicateSession = Guid.NewGuid();
            
            sessionPolicy.AddSession(duplicateSession);

            Assert.Throws<Exception>(() =>
            {
                sessionPolicy.AddSession(duplicateSession);
            });
        }

        [Fact]
        public void MakeSureWhenRemoveIsCalledTheSessionIsReturnCorrectly()
        {
            var sessionPolicy = new RoundRobinSessionPolicy();

            var sessionList = new [] {Guid.NewGuid(), Guid.NewGuid()}; 
            
            sessionPolicy.AddSession(sessionList[0]);
            sessionPolicy.AddSession(sessionList[1]);
            
            _ = sessionPolicy.GetNextSession();

            sessionPolicy.RemoveSession(sessionList[0]);

            var session = sessionPolicy.GetNextSession();
            
            Assert.Equal(sessionList[1], session);
        }
    }
}