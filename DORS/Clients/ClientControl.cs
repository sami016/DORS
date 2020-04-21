using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ActionSerialization;
using DORS.Shared;
using Lidgren.Network;
using Newtonsoft.Json;

namespace DORS.Clients
{
    /// <summary>
    /// Main client controls for connecting, disconnecting and server hopping.
    /// </summary>
    public class ClientControl: IClientControl
    {
        private readonly DorsClientConfiguration _configuration;
        private ClientSession _activeSession;

        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        public bool IsHopping { get; private set; }

        /// <summary>
        /// Fired upon client connection.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Fired upon client disconnect.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Fired upon client successful hop.
        /// </summary>
        public event EventHandler Hopped;

        /// <summary>
        /// Fired upon client unsuccessful hop.
        /// </summary>
        public event EventHandler HopFailed;

        /// <summary>
        /// Reserved for raising errors to be handled.
        /// </summary>
        public event EventHandler<Exception> Errored;


        /// <summary>
        /// Fired whenever a message is received.
        /// </summary>
        public event EventHandler<object> MessageReceived;

        public ClientControl(DorsClientConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Connect(string host, int port, object authMessage = null)
        {
            AsyncConnect(host, port, authMessage).Start();
        }

        public async Task<bool> AsyncConnect(string host, int port, object authMessage)
        {
            if (IsConnecting || IsHopping)
            {
                throw new Exception("Already connecting");
            }
            IsConnecting = true;
            var session = new ClientSession(_configuration, host, port, authMessage);
            session.Disconnected += HandleDisconnected;
            session.Connected += HandleConnected;
            session.Errored += HandleErrored;
            session.MessageReceived += HandleMessageReceived;
            
            var success = await session.AsyncConnect();
            if (success)
            {
                _activeSession = session;

                IsConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
            } 
            else
            {
                session.Disconnected -= HandleDisconnected;
                session.Connected -= HandleConnected;
                session.Errored -= HandleErrored;
                session.MessageReceived -= HandleMessageReceived;
            }
            IsConnecting = false;
            return success;
        }

        private void HandleMessageReceived(object sender, object message)
        {
            MessageReceived?.Invoke(this, message);
        }

        private void HandleErrored(object sender, Exception e)
        {
            Errored?.Invoke(this, e);
        }

        private void HandleDisconnected(object sender, EventArgs e)
        {
            IsConnected = false;
            Disconnected?.Invoke(this, e);
        }

        private void HandleConnected(object sender, EventArgs e)
        {
            IsConnected = true;
            Connected?.Invoke(this, e);
        }

        public bool WaitHop(string host, int port, object authMessage = null)
        {
            if (_activeSession == null || !_activeSession.IsConnected)
            {
                throw new Exception("Must be connected in order to perform hop");
            }
            if (IsHopping)
            {
                throw new Exception("Hop has already been requested");
            }

            IsHopping = true;

            try
            {
                // Create a new session, and let it connect.
                var pendingSession = new ClientSession(_configuration, host, port, authMessage);
                var success = pendingSession.ProcessIncomingMessagesStart();
                if (success)
                {
                    // Connection successful - 
                    _activeSession.Disconnected -= HandleDisconnected;
                    _activeSession.Connected -= HandleConnected;
                    _activeSession.Errored -= HandleErrored;
                    _activeSession.MessageReceived -= HandleMessageReceived;
                    _activeSession.Disconnect();
                    _activeSession.Dispose();

                    _activeSession = pendingSession;
                    _activeSession.Disconnected += HandleDisconnected;
                    _activeSession.Connected += HandleConnected;
                    _activeSession.Errored += HandleErrored;
                    _activeSession.MessageReceived += HandleMessageReceived;

                    Hopped?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    pendingSession.Dispose();

                    HopFailed?.Invoke(this, EventArgs.Empty);
                }

                IsHopping = false;
                return success;
            }
            catch (Exception ex)
            {
                IsHopping = false;
                Errored?.Invoke(this, ex);
                return false;
            }
        }


        public void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered,
            int channelSequence = 0)
        {
            if (_activeSession == null || !_activeSession.IsConnected)
            {
                throw new Exception("Not connected");
            }
            _activeSession.Send(action, method, channelSequence);
        }

        public void Disconnect()
        {
            if (_activeSession != null)
            {
                if (_activeSession.IsConnected)
                {
                    _activeSession.Disconnect();
                }
                _activeSession.Dispose();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
