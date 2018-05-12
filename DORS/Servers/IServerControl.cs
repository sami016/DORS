using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Servers
{
    public interface IServerControl : IDisposable
    {
        /// <summary>
        /// Send a message to a specific connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="action"></param>
        /// <param name="method"></param>
        void Send(NetConnection connection, object action,
            NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        /// <param name="action">action</param>
        /// <param name="method">method</param>
        void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);

        /// <summary>
        /// Gets reference to underlying server.
        /// </summary>
        NetServer NetServer { get; }
    }
}
