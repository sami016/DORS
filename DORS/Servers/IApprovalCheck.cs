using System;
using System.Collections.Generic;
using System.Text;

namespace DORS.Servers
{
    public interface IApprovalCheck
    {
        bool IsApproved(RemoteConnection remoteConnection, object approvalMessageAction);
    }
}
