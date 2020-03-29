using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Shared
{
    public interface ISerializationStrategy
    {
        object Deserialize(NetIncomingMessage incomingMessage);
        void Serialize(object action, NetOutgoingMessage outgoingMessage);
    }
}
