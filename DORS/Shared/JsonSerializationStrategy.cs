using System;
using System.Collections.Generic;
using System.Text;
using ActionSerialization;

namespace DORS.Shared
{
    public class JsonSerializationStrategy : ISerializationStrategy
    {
        public ActionDeserializer Deserializer { get; }
        public ActionSerializer Serializer { get; }

        public JsonSerializationStrategy()
        {
            Deserializer = msg => ActionConvert.Deserialize(msg.ReadString());
            Serializer = (action, msg) => msg.Write(ActionConvert.Serialize(action));
        }
    }
}
