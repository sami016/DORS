using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Servers
{
    /// <summary>
    /// Creates a client registry that manages remote client state, based on server control events.
    /// </summary>
    public class RemoteClientRegistry
    {
        private readonly IDictionary<long, RemoteClient> _clients = new ConcurrentDictionary<long, RemoteClient>();

        //public event EventHandler<T> Connected;
        //public event EventHandler<T> Disconnected;
        //public event EventHandler<InboundClientAction<T>> MessageReceived;

        //public RemoteClientRegistry(ServerControl serverControl)
        //{
        //    serverControl.Connected += OnServerPeerConnected;
        //    serverControl.Disconnected += OnServerPeerDisconnected;
        //    serverControl.MessageReceived += OnServerPeerMessageReceived;
        //}

       

        //private void OnServerPeerMessageReceived(object sender, InboundAction inboundAction)
        //{
        //    var id = inboundAction.ClientId;
        //    if (_clients.ContainsKey(id))
        //    {
        //        MessageReceived?.Invoke(
        //            this,
        //            new InboundClientAction<T>(
        //                _clients[id],
        //                inboundAction.Action
        //            )
        //        );
        //    }
        //}

        public RemoteClient this[long id]
        {
            get
            {
                if (_clients.ContainsKey(id))
                {
                    return _clients[id];
                }
                return null;
            }

            set => _clients[id] = value;
        }

        public void Remove(long id)
        {
            _clients.Remove(id);
        }

        public void Remove(RemoteClient client)
        {
            _clients.Remove(client.ClientId);
        }
    }
}
