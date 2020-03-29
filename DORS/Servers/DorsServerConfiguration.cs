using System;
using System.Collections.Generic;
using System.Text;
using DORS.Shared;
using Lidgren.Network;

namespace DORS.Servers
{
    public class DorsServerConfiguration : DorsConfiguration
    {
        public ApprovalCheck ApprovalCheck { get; set; }

        public DorsServerConfiguration()
        {
        }

        protected override NetPeerConfiguration CreateNetPeerConfig()
        {
            var config = base.CreateNetPeerConfig();
            if (ApprovalCheck != null)
            {
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            }
            return config;
        }

    }
}
