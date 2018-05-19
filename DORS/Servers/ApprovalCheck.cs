using System;
using System.Collections.Generic;
using System.Text;

namespace DORS.Servers
{
    public delegate bool ApprovalCheck(RemoteClient remoteClient, object approvalMessageAction);
    //public interface IApprovalCheck
    //{
    //    bool IsApproved(RemoteClient remoteClient, object approvalMessageAction);
    //}
}
