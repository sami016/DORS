using System;
using System.Collections.Generic;
using System.Text;

namespace DORS.Interfaces
{
    public interface IMessageReceiver
    {
        IDisposable Receive<T>(Func<T> receiverFunc);
    }
}
