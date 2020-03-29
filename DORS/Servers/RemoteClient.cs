using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Servers
{
    public class RemoteClient : IDisposable
    {
        internal ServerControl ServerControl { get; set; }
        public NetConnection Connection { get; internal set; }

        public event EventHandler<object> MessageReceived;

        public long ClientId => Connection.RemoteUniqueIdentifier;

        // Initialise called after the client id has been set, but prior being added to registry.
        public virtual void Initialise()
        {
            
        }

        // Dispose called automatically during disconnect.
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Send a message to the remote client.
        /// </summary>
        /// <param name="connection">connection</param>
        /// <param name="action">action</param>
        /// <param name="method">method</param>
        public void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            ServerControl.Send(Connection, action, method);
        }

        internal void OnMessageReceived(object message)
        {
            MessageReceived?.Invoke(this, message);
        }

    }
}
