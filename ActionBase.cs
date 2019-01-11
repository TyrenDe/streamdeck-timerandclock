using Newtonsoft.Json.Linq;
using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimerAndClock
{
    public abstract class ActionBase
    {
        public abstract Task LoadAsync(StreamDeckConnection connection, string action, string context, JObject settings);
        public abstract Task SaveAsync();
        public abstract Task KeyDownAsync();
        public abstract Task KeyUpAsync();
        public abstract Task ProcessPropertyInspectorAsync(SendToPluginEvent propertyInspectorEvent);
        public abstract Task RunTickAsync();
    }
}
