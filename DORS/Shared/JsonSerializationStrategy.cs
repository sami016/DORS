using System;
using System.Collections.Generic;
using System.Text;
using ActionSerialization;
using Lidgren.Network;

namespace DORS.Shared
{
    public class JsonSerializationStrategy : ISerializationStrategy
    {

        public JsonSerializationStrategy()
        {
        }

        public object Deserialize(NetIncomingMessage incomingMessage)
        {
            return ActionConvert.Deserialize(incomingMessage.ReadString());
        }

        public void Serialize(object action, NetOutgoingMessage outgoingMessage)
        {
            outgoingMessage.Write(ActionConvert.Serialize(action));
        }
    }
}
