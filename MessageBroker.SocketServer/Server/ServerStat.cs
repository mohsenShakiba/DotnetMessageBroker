using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public class ServerStat: IDisposable
    {

        private int _rps;
        private Timer _timer;
        private string _template;
        private TimeSpan? period;
        private readonly ILogger<ServerStat> _logger;

        public ServerStat(ILogger<ServerStat> logger)
        {
            _logger = logger;
        }

        public void LogPeriodic(string template, TimeSpan period)
        {
            _template = template;
            _timer = new(OnTimerTick, null, TimeSpan.Zero, period);
        }

        public void Reset()
        {
            _rps = 0;
        }

        public void Inc()
        {
            Interlocked.Increment(ref _rps);
        }

        public void Desc()
        {
            Interlocked.Decrement(ref _rps);
        }

        private void OnTimerTick(object _)
        {
            _logger.LogInformation(_template, _rps);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
