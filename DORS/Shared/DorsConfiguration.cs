using System;
using System.Collections.Generic;
using System.Text;
using ActionSerialization;
using Lidgren.Network;

namespace DORS.Shared
{
    /// <summary>
    /// DORS configuration.
    /// </summary>
    public abstract class DorsConfiguration
    {
        public Func<NetIncomingMessage, object> ActionDeserialize { get; set; }
        public Action<object, NetOutgoingMessage> ActionSerialize { get; set; }
        public NetPeerConfiguration PeerConfiguration { get; private set; }

        public int LocalPort
        {
            get => PeerConfiguration.Port;
            set => PeerConfiguration.Port = value;
        }

        protected DorsConfiguration(string appIdentifier)
        {
            PeerConfiguration = new NetPeerConfiguration(appIdentifier);
            // Default: action convert json.
            ActionDeserialize = msg => ActionConvert.Deserialize(msg.ReadString());
            ActionSerialize = (action, msg) => msg.Write(ActionConvert.Serialize(action));
        }
    }
}
