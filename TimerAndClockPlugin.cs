using Newtonsoft.Json.Linq;
using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TimerAndClock
{
    public class TimerAndClockPlugin
    {
        private static readonly Dictionary<string, Type> s_ActionList = new Dictionary<string, Type>();

        private StreamDeckConnection m_Connection;
        private readonly ManualResetEvent m_ConnectEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent m_DisconnectEvent = new ManualResetEvent(false);
        private readonly Dictionary<string, ActionBase> m_Actions = new Dictionary<string, ActionBase>();
        private readonly SemaphoreSlim m_ActionsLock = new SemaphoreSlim(1);

        static TimerAndClockPlugin()
        {
            Type[] types = typeof(TimerAndClockPlugin).Assembly.GetTypes();
            foreach (Type type in types)
            {
                object[] attributes = type.GetCustomAttributes(typeof(ActionAttribute), false);
                if (attributes.Length > 0)
                {
                    ActionAttribute actionAttribute = attributes[0] as ActionAttribute;
                    if (actionAttribute != null)
                    {
                        s_ActionList[actionAttribute.ActionName.ToLower()] = type;
                    }
                }
            }
        }

        public async Task RunAsync(int port, string uuid, string registerEvent)
        {
            m_Connection = new StreamDeckConnection(port, uuid, registerEvent);

            m_Connection.OnConnected += Connection_OnConnected;
            m_Connection.OnDisconnected += Connection_OnDisconnected;
            m_Connection.OnKeyDown += Connection_OnKeyDown;
            m_Connection.OnKeyUp += Connection_OnKeyUp;
            m_Connection.OnWillAppear += Connection_OnWillAppear;
            m_Connection.OnWillDisappear += Connection_OnWillDisappear;
            m_Connection.OnSendToPlugin += Connection_OnSendToPlugin;

            // Start the connection
            m_Connection.Run();

            // Wait for up to 10 seconds to connect, if it fails, the app will exit
            if (m_ConnectEvent.WaitOne(TimeSpan.FromSeconds(10)))
            {
                // We connected, loop every 1/2 second until we disconnect
                while (!m_DisconnectEvent.WaitOne(TimeSpan.FromMilliseconds(500)))
                {
                    await RunTickAsync();
                }
            }
            else
            {
                Console.WriteLine("Plugin failed to connect to Stream Deck");
            }
        }

        private async void Connection_OnSendToPlugin(object sender, StreamDeckEventReceivedEventArgs<SendToPluginEvent> e)
        {
            await m_ActionsLock.WaitAsync();
            try
            {
                if (m_Actions.ContainsKey(e.Event.Context.ToLower()))
                {
                    await m_Actions[e.Event.Context.ToLower()].ProcessPropertyInspectorAsync(e.Event);
                }
            }
            finally
            {
                m_ActionsLock.Release();
            }
        }

        private async void Connection_OnWillDisappear(object sender, StreamDeckEventReceivedEventArgs<WillDisappearEvent> e)
        {
            await m_ActionsLock.WaitAsync();
            try
            {
                if (m_Actions.ContainsKey(e.Event.Context.ToLower()))
                {
                    await m_Actions[e.Event.Context.ToLower()].SaveAsync();
                    m_Actions.Remove(e.Event.Context.ToLower());
                }
            }
            finally
            {
                m_ActionsLock.Release();
            }
        }

        private async void Connection_OnWillAppear(object sender, StreamDeckEventReceivedEventArgs<WillAppearEvent> e)
        {
            await m_ActionsLock.WaitAsync();
            try
            {
                if (s_ActionList.ContainsKey(e.Event.Action.ToLower()))
                {
                    ActionBase action = Activator.CreateInstance(s_ActionList[e.Event.Action.ToLower()]) as ActionBase;
                    await action.LoadAsync(m_Connection, e.Event.Action, e.Event.Context, e.Event.Payload.Settings);
                    m_Actions[e.Event.Context.ToLower()] = action;
                }
            }
            finally
            {
                m_ActionsLock.Release();
            }
        }

        private async void Connection_OnKeyDown(object sender, StreamDeckEventReceivedEventArgs<KeyDownEvent> e)
        {
            await m_ActionsLock.WaitAsync();
            try
            {
                if (m_Actions.ContainsKey(e.Event.Context.ToLower()))
                {
                    await m_Actions[e.Event.Context.ToLower()].KeyDownAsync();
                }
            }
            finally
            {
                m_ActionsLock.Release();
            }
        }

        private async void Connection_OnKeyUp(object sender, StreamDeckEventReceivedEventArgs<KeyUpEvent> e)
        {
            await m_ActionsLock.WaitAsync();
            try
            {
                if (m_Actions.ContainsKey(e.Event.Context.ToLower()))
                {
                    await m_Actions[e.Event.Context.ToLower()].KeyUpAsync();
                }
            }
            finally
            {
                m_ActionsLock.Release();
            }
        }

        private async Task RunTickAsync()
        {
            // If the plugin needs to do anything, this runs once per second.
            // It could be used to ping to see if the app has arrived, etc.
            await m_ActionsLock.WaitAsync();
            try
            {
                foreach (ActionBase action in m_Actions.Values)
                {
                    await action.RunTickAsync();
                }
            }
            finally
            {
                m_ActionsLock.Release();
            }
        }

        private async void Connection_OnDisconnected(object sender, EventArgs e)
        {
            await m_ActionsLock.WaitAsync();
            try
            {
                m_Actions.Clear();
            }
            finally
            {
                m_ActionsLock.Release();
            }

            m_DisconnectEvent.Set();
        }

        private void Connection_OnConnected(object sender, EventArgs e)
        {
            m_ConnectEvent.Set();
        }
    }
}