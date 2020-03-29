using System;
using System.Collections.Generic;
using System.Threading;
using ActionSerialization;
using Lidgren.Network;

namespace DORS.Servers
{
    public class ServerControl : IDisposable
    {
        private readonly CancellationTokenSource _cancelSource;
        private readonly DorsServerConfiguration _configuration;

        public NetServer NetServer { get; private set; }

        public event EventHandler<RemoteClient> Connected;
        public event EventHandler<RemoteClient> ApprovalGranted;
        public event EventHandler<RemoteClient> ApprovalDenied;
        public event EventHandler<RemoteClient> Disconnected;
        public event EventHandler<RemoteClientAction> MessageReceived;

        private readonly RemoteClientRegistry _remoteClientRegistry = new RemoteClientRegistry();

        public ServerControl(DorsServerConfiguration configuration)
        {
            _configuration = configuration;
            _cancelSource = new CancellationTokenSource();
        }

        public void Start()
        {
            NetServer = new NetServer(_configuration.PeerConfiguration);
            NetServer.Start();

            new Thread(Process).Start();
        }

        private void Process()
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                NetIncomingMessage message;
                while ((message = NetServer.ReadMessage()) != null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.ConnectionApproval:
                            // Deserialize message - then either approval or deny using approval method.
                            var action = _configuration.SerializationStrategy.Deserialize(message);
                            var remoteClient = _remoteClientRegistry[message.SenderConnection.RemoteUniqueIdentifier];
                            if (action != null 
                                &&_configuration.ApprovalCheck(remoteClient, action))
                            {
                                message.SenderConnection.Approve();
                                ApprovalGranted?.Invoke(this, remoteClient);
                            }
                            else
                            {
                                message.SenderConnection.Deny();
                                ApprovalDenied?.Invoke(this, remoteClient);
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            var status = (NetConnectionStatus) message.ReadByte();
                            OnStatusChanged(message, status);
                            break;
                        case NetIncomingMessageType.Data:
                            OnDataReceived(message);
                            break;
                    }
                }
            }   
        }

        private void OnDataReceived(NetIncomingMessage message)
        {
            var action = _configuration.SerializationStrategy.Deserialize(message);
            var session = _remoteClientRegistry[message.SenderConnection.RemoteUniqueIdentifier];
            MessageReceived?.Invoke(this, new RemoteClientAction(session, action));
        }

        private void OnStatusChanged(NetIncomingMessage message, NetConnectionStatus status)
        {
            switch (status)
            {
                case NetConnectionStatus.Connected:
                    OnConnected(message);
                    break;
                case NetConnectionStatus.Disconnected:
                    OnDisconnected(message);
                    break;
            }
        }

        // When peer connects, create them a remote client.
        private void OnConnected(NetIncomingMessage message)
        {
            var id = message.SenderConnection.RemoteUniqueIdentifier;

            var instance = new RemoteClient();
            instance.Connection = message.SenderConnection;
            instance.Initialise();

            _remoteClientRegistry[id] = instance;

            Connected?.Invoke(this, instance);
        }


        // When peer disconnects, dispose their remote client.
        private void OnDisconnected(NetIncomingMessage message)
        {
            var id = message.SenderConnection.RemoteUniqueIdentifier;

            var instance = _remoteClientRegistry[id];
            Disconnected?.Invoke(this, instance);
            if (instance != null)
            {
                instance.Dispose();
                _remoteClientRegistry.Remove(id);
            }
        }

        /// <summary>
        /// Send a message to a specific connection.
        /// </summary>
        /// <param name="connection">connection</param>
        /// <param name="action">action</param>
        /// <param name="method">method</param>
        public void Send(NetConnection connection, object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            var message = NetServer.CreateMessage();
            _configuration.SerializationStrategy.Serialize(action, message);
            NetServer.SendMessage(message, connection, method);
        }

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        /// <param name="action">action</param>
        /// <param name="method">method</param>
        public void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            var message = NetServer.CreateMessage();
            _configuration.SerializationStrategy.Serialize(action, message);
            NetServer.SendToAll(message, method);
        }

        public void Dispose()
        {
            _cancelSource.Cancel();
        }
    }
}
