using System;
using System.Collections.Generic;
using System.Text;
using DORS.Interfaces;
using DORS.Shared;
using Lidgren.Network;

namespace DORS.Servers
{
    public class RemoteConnection : IMessageSender, IDisposable
    {
        internal ServerControl ServerControl { get; set; }
        public NetConnection Connection { get; internal set; }
        public PolymorphicDispatcher PolymorphicDispatcher { get; } = new PolymorphicDispatcher();
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
        /// <param name="message">action</param>
        /// <param name="method">method</param>
        public void Send(object message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            ServerControl.Send(Connection, message, method);
        }

        internal void OnMessageReceived(object message)
        {
            MessageReceived?.Invoke(this, message);
            PolymorphicDispatcher.Dispatch(message);
        }

    }
}
