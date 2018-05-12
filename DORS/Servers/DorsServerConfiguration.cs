using System;
using System.Collections.Generic;
using System.Text;
using DORS.Shared;
using Lidgren.Network;

namespace DORS.Servers
{
    public class DorsServerConfiguration : DorsConfiguration
    {
        public IApprovalCheck ApprovalCheck { get; private set; }

        public DorsServerConfiguration(string appIdentifier) : base(appIdentifier)
        {
        }

        public DorsServerConfiguration EnableApproval(IApprovalCheck approvalCheck)
        {
            ApprovalCheck = approvalCheck;
            PeerConfiguration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            return this;
        }

        
    }
}
