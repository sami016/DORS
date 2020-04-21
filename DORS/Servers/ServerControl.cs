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
        private readonly NetPeerConfiguration _netPeerConfiguration;
        private readonly DorsServerConfiguration _configuration;

        public NetServer NetServer { get; private set; }

        public event EventHandler<RemoteConnection> Connected;
        public event EventHandler<RemoteConnection> ApprovalGranted;
        public event EventHandler<RemoteConnection> ApprovalDenied;
        public event EventHandler<RemoteConnection> Disconnected;

        private readonly RemoteConnectionRegistry _remoteClientRegistry = new RemoteConnectionRegistry();

        public IEnumerable<RemoteConnection> RemoteClients => _remoteClientRegistry.All;

        private IApprovalCheck _approvalCheck;
        public IApprovalCheck ApprovalCheck
        {
            get => _approvalCheck;
            set
            {
                if (_approvalCheck == null)
                {
                    _netPeerConfiguration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                }
                _approvalCheck = value;
            }
        }

        public ServerControl(DorsServerConfiguration configuration, int port)
        {
            _configuration = configuration;
            _cancelSource = new CancellationTokenSource();
            _netPeerConfiguration = new NetPeerConfiguration(configuration.AppIdentifier);
            _netPeerConfiguration.Port = port;
        }

        public void Start()
        {
            NetServer = new NetServer(_netPeerConfiguration);
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
                            var approvalMessage = _configuration.SerializationStrategy.Deserialize(message);
                            var remoteClient = GetRemoteConnection(message);
                            if (approvalMessage != null 
                                && ApprovalCheck.IsApproved(remoteClient, approvalMessage))
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
            session.OnMessageReceived(action);
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

        private RemoteConnection GetRemoteConnection(NetIncomingMessage netIncomingMessage)
        {
            var id = netIncomingMessage.SenderConnection.RemoteUniqueIdentifier;

            if (_remoteClientRegistry[id] != null)
            {
                return _remoteClientRegistry[id];
            }

            var remoteConnection = new RemoteConnection();
            remoteConnection.ServerControl = this;
            remoteConnection.Connection = netIncomingMessage.SenderConnection;
            remoteConnection.Initialise();

            _remoteClientRegistry[id] = remoteConnection;
            return remoteConnection;
        }

        // When peer connects, create them a remote client.
        private void OnConnected(NetIncomingMessage message)
        {
            Connected?.Invoke(this, GetRemoteConnection(message));
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
