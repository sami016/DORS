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
        public NetPeerConfiguration PeerConfiguration { get; private set; }
        public ISerializationStrategy SerializationStrategy { get; set; }

        public int LocalPort
        {
            get => PeerConfiguration.Port;
            set => PeerConfiguration.Port = value;
        }

        protected DorsConfiguration(string appIdentifier)
        {
            PeerConfiguration = new NetPeerConfiguration(appIdentifier);
            SerializationStrategy = new BinarySerializationStrategy();
        }
    }
}
