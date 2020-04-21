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

        public ISerializationStrategy SerializationStrategy { get; set; } = new BinarySerializationStrategy();


        public DorsConfiguration()
        {
            SerializationStrategy = new BinarySerializationStrategy();
        }

    }
}
