using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORS.Interfaces
{
    public interface IMessageSender
    {
        void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);
    }
}
