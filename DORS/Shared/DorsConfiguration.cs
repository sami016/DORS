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
        
        public string AppIdentifier { get; set; }

        private Lazy<NetPeerConfiguration> _netPeerConfiguration;

        public NetPeerConfiguration PeerConfiguration => _netPeerConfiguration.Value;
        public ISerializationStrategy SerializationStrategy { get; set; } = new BinarySerializationStrategy();

        public int LocalPort
        {
            get => PeerConfiguration.Port;
            set => PeerConfiguration.Port = value;
        }

        public DorsConfiguration()
        {
            _netPeerConfiguration = new Lazy<NetPeerConfiguration>(CreateNetPeerConfig);
            SerializationStrategy = new BinarySerializationStrategy();
        }

        protected virtual NetPeerConfiguration CreateNetPeerConfig()
        {
            var config = new NetPeerConfiguration(AppIdentifier);
            return config;
        }
    }
}
