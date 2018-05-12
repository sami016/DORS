using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;

namespace DORS.Clients
{
    public interface IClientControl : IDisposable
    {
        void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int channelSequence = 0);
    }
}
