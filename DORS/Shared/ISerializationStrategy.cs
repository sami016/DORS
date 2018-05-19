using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Shared
{
    public delegate object ActionDeserializer(NetIncomingMessage incomingMessage);
    public delegate void ActionSerializer(object action, NetOutgoingMessage outgoingMessage);

    public interface ISerializationStrategy
    {
        ActionDeserializer Deserializer { get; }
        ActionSerializer Serializer { get; }
    }
}
