using System;
using System.Collections.Generic;
using System.Threading;
using ActionSerialization;
using Lidgren.Network;

namespace DORS.Servers
{
    public class ServerControl : IServerControl
    {
        private readonly CancellationTokenSource _cancelSource;
        private readonly DorsServerConfiguration _configuration;
        private NetServer _netServer;

        public NetServer NetServer => _netServer;

        public event EventHandler<NetConnection> Connected;
        public event EventHandler<NetConnection> Disconnected;
        public event EventHandler<object> MessageReceived;

        public ServerControl(DorsServerConfiguration configuration)
        {
            _configuration = configuration;
            _cancelSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _netServer = new NetServer(_configuration.PeerConfiguration);
            _netServer.Start();

            new Thread(Process).Start();
        }

        private void Process()
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                NetIncomingMessage message;
                while ((message = _netServer.ReadMessage()) != null)
                {
                    object action;
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.ConnectionApproval:
                            // Deserialize message - then either approval or deny using approval method.
                            action = _configuration.ActionDeserialize(message);
                            if (action != null 
                                &&_configuration.ApprovalCheck.IsApproved(action))
                            {
                                message.SenderConnection.Approve();
                            }
                            else
                            {
                                message.SenderConnection.Deny();
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            var status = (NetConnectionStatus) message.ReadByte();
                            OnStatusChanged(message, status);
                            break;
                        case NetIncomingMessageType.Data:
                            action = _configuration.ActionDeserialize(message);
                            MessageReceived?.Invoke(this, action);
                            break;
                    }
                }
            }   
        }

        private void OnStatusChanged(NetIncomingMessage message, NetConnectionStatus status)
        {
            switch (status)
            {
                case NetConnectionStatus.Connected:
                    Connected?.Invoke(this, message.SenderConnection);
                    break;
                case NetConnectionStatus.Disconnected:
                    Disconnected?.Invoke(this, message.SenderConnection);
                    break;
            }
        }


        public void Send(NetConnection connection, object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            var message = _netServer.CreateMessage();
            _configuration.ActionSerialize(action, message);
            _netServer.SendMessage(message, connection, method);
        }

        public void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            var message = _netServer.CreateMessage();
            _configuration.ActionSerialize(action, message);
            _netServer.SendToAll(message, method);
        }

        public void Dispose()
        {
            _cancelSource.Cancel();
        }
    }
}
