using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;

namespace TimerAndClock
{
    [Action("com.tyren.timerandclock.timer")]
    public class TimerAction : ActionBase
    {
        private StreamDeckConnection m_Connection;
        private string m_Context;
        private DateTimeOffset? m_ResetTime;
        private Stopwatch m_Stopwatch = new Stopwatch();

        public override Task KeyDownAsync()
        {
            m_ResetTime = DateTimeOffset.UtcNow.AddSeconds(1);
            return Task.FromResult(0);
        }

        public override Task KeyUpAsync()
        {
            return Task.Run(() =>
            {
                if (m_ResetTime.HasValue)
                {
                    if (m_ResetTime.Value < DateTimeOffset.UtcNow)
                    {
                        // Reset!
                        m_ResetTime = null;
                        m_Stopwatch.Reset();
                        return;
                    }
                }

                // Otherwise, toggle running state
                if (m_Stopwatch.IsRunning)
                {
                    m_Stopwatch.Stop();
                }
                else
                {
                    m_Stopwatch.Start();
                }
            });
        }

        public override Task LoadAsync(StreamDeckConnection connection, string action, string context, JObject settings)
        {
            m_Connection = connection;
            m_Context = context;
            return Task.FromResult(0);
        }

        public override Task ProcessPropertyInspectorAsync(SendToPluginEvent propertyInspectorEvent)
        {
            return Task.FromResult(0);
        }

        public override async Task RunTickAsync()
        {
            TimeSpan elapsed = m_Stopwatch.Elapsed;
            // TODO: Apply formatting
            await m_Connection.SetTitleAsync(m_Stopwatch.Elapsed.ToString(), m_Context, SDKTarget.HardwareAndSoftware);
        }

        public override Task SaveAsync()
        {
            return Task.FromResult(0);
        }
    }
}
