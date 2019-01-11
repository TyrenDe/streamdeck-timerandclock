using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;

namespace TimerAndClock
{
    public class ClockActionSettings
    {
        [JsonProperty("dateFormat")]
        public string DateFormat { get; set; }

        [JsonProperty("timeFormat")]
        public string TimeFormat { get; set; }
    }

    [Action("com.tyren.timerandclock.clock")]
    public class ClockAction : ActionBase
    {
        private StreamDeckConnection m_Connection;
        private string m_Action;
        private string m_Context;
        private ClockActionSettings m_Settings;

        public override Task KeyDownAsync()
        {
            // Nothing to do
            return Task.FromResult(0);
        }

        public override Task KeyUpAsync()
        {
            // Nothing to do
            return Task.FromResult(0);
        }

        public override Task LoadAsync(StreamDeckConnection connection, string action, string context, JObject settings)
        {
            m_Connection = connection;
            m_Action = action;
            m_Context = context;
            m_Settings = settings.ToObject<ClockActionSettings>();

            return Task.FromResult(0);
        }

        public override async Task ProcessPropertyInspectorAsync(SendToPluginEvent propertyInspectorEvent)
        {
            switch (propertyInspectorEvent.Payload["property_inspector"].ToString().ToLower())
            {
                case "propertyinspectorconnected":
                    // Send settings to Property Inspector
                    await m_Connection.SendToPropertyInspectorAsync(m_Action, JObject.FromObject(m_Settings), m_Context);
                    break;
                case "propertyinspectorwilldisappear":
                    await SaveAsync();
                    break;
                case "updatesettings":
                    m_Settings = propertyInspectorEvent.Payload.ToObject<ClockActionSettings>();
                    await SaveAsync();
                    break;
            }
        }

        public override async Task RunTickAsync()
        {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(m_Settings.DateFormat))
            {
                text = DateTimeOffset.Now.ToString(m_Settings.DateFormat);
            }

            if (!string.IsNullOrEmpty(m_Settings.TimeFormat))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    text += "\n";
                }

                text += DateTimeOffset.Now.ToString(m_Settings.TimeFormat);
            }

            await m_Connection.SetTitleAsync(text, m_Context, SDKTarget.HardwareAndSoftware);
        }

        public override async Task SaveAsync()
        {
            await m_Connection.SetSettingsAsync(JObject.FromObject(m_Settings), m_Context);
        }
    }
}
