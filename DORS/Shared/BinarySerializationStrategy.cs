using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ActionSerialization;
using Lidgren.Network;

namespace DORS.Shared
{
    public class BinarySerializationStrategy : ISerializationStrategy
    {
        public ActionDeserializer Deserializer { get; }
        public ActionSerializer Serializer { get; }

        private BinaryFormatter _binaryFormatter;

        public BinarySerializationStrategy()
        {
            Deserializer = msg => ActionConvert.Deserialize(msg.ReadString());
            Serializer = (action, msg) => msg.Write(ActionConvert.Serialize(action));
            _binaryFormatter = new BinaryFormatter();

        }

        private void Serialize(object action, NetOutgoingMessage message)
        {
            using (var memoryStream = new MemoryStream())
            {
                _binaryFormatter.Serialize(memoryStream, action);
                message.Write((int)memoryStream.Length);
                message.Write(memoryStream.ToArray());
            }
        }

        private object Deserialize(NetIncomingMessage message)
        {
            using (var memoryStream = new MemoryStream())
            {
                var length = message.ReadInt32();
                var data = message.ReadBytes((int)length);
                memoryStream.Write(data, 0, data.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return _binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
