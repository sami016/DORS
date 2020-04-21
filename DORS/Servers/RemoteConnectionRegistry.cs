using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Servers
{
    /// <summary>
    /// Creates a client registry that manages remote connection state, based on server control events.
    /// </summary>
    public class RemoteConnectionRegistry
    {
        private readonly IDictionary<long, RemoteConnection> _clients = new ConcurrentDictionary<long, RemoteConnection>();

        public IEnumerable<RemoteConnection> All => _clients.Values;

        public RemoteConnection this[long id]
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

        public void Remove(RemoteConnection client)
        {
            _clients.Remove(client.ClientId);
        }
    }
}
