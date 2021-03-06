﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ActionSerialization;
using Lidgren.Network;

namespace DORS.Clients
{
    public class ClientSession : IDisposable
    {
        private readonly NetPeer _netClient;
        private readonly NetConnection _connection;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _isConnected;
        private readonly DorsClientConfiguration _configuration;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                if (value)
                {
                    Connected?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public NetConnection Connection => _connection;

        /// <summary>
        /// Fired upon client connection.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Fired upon client disconnect.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Reserved for raising errors to be handled.
        /// </summary>
        public event EventHandler<Exception> Errored;

        /// <summary>
        /// Fired whenever a message is received.
        /// </summary>
        public event EventHandler<object> MessageReceived;

        public ClientSession(DorsClientConfiguration configuration, string host, int port, object authMessage)
        {
            _configuration = configuration;
            _cancellationTokenSource = new CancellationTokenSource();
            // Create net client.
            _netClient = new NetClient(new NetPeerConfiguration(_configuration.AppIdentifier));
            _netClient.Start();

            // Prepare hail message.
            NetOutgoingMessage hailMessage = null;
            if (authMessage != null)
            {
                hailMessage = _netClient.CreateMessage();
                _configuration.SerializationStrategy.Serialize(authMessage, hailMessage);
                hailMessage.Write(hailMessage);
            }
            // Initialise connection.
            _connection = _netClient.Connect(host, port, hailMessage);
        }

        /// <summary>
        /// Begins the main process of handling incoming messages.
        /// </summary>
        public void ProcessIncomingMessages()
        {
            new Thread(Process).Start();
        }

        public Task<bool> AsyncConnect()
        {
            return Task.Run((Func<bool>)ProcessMessagesStart);
        }

        /// <summary>
        /// Process only status changed messages until either a connected or a not connected.
        /// </summary>
        /// <param name="connected">connected succefully</param>
        public bool ProcessMessagesStart()
        {
            // Read messages until we either connect or disconnect.
            foreach (var message in MessageStream())
            {
                //ProcessMessage(message);
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus)message.PeekByte();
                        if (status == NetConnectionStatus.Connected)
                        {
                            IsConnected = true;
                            return true;
                        }
                        else if (status == NetConnectionStatus.Disconnected)
                        {
                            IsConnected = false;
                            return false;
                        }
                        break;
                }
            }
            IsConnected = false;
            return false;
        }

        /// <summary>
        /// Creates an stream of incoming messages.
        /// Stream will end whenever cancellation is requested.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<NetIncomingMessage> MessageStream()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                NetIncomingMessage message;
                while ((message = _netClient.ReadMessage()) != null)
                {
                    yield return message;
                }
            }
        }

        /// <summary>
        /// Processes messages from the message stream.
        /// </summary>
        private void Process()
        {
            foreach (var message in MessageStream())
            {
                ProcessMessage(message);
            }
        }

        /// <summary>
        /// Processes an inbound message.
        /// </summary>
        /// <param name="message">message</param>
        private void ProcessMessage(NetIncomingMessage message)
        {
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var status = (NetConnectionStatus)message.PeekByte();
                    if (!IsConnected)
                    {
                        if (status == NetConnectionStatus.Connected)
                        {
                            _isConnected = true;
                        }
                    }
                    else
                    {
                        if (status != NetConnectionStatus.Connected)
                        {
                            IsConnected = false;
                        }
                    }
                    break;
                case NetIncomingMessageType.Data:
                    var action = _configuration.SerializationStrategy.Deserialize(message);
                    MessageReceived?.Invoke(this, action);
                    break;
            }
        }


        public void Send(object action, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int channelSequence = 0)
        {
            var message = _netClient.CreateMessage();
            _configuration.SerializationStrategy.Serialize(action, message);
            _connection.SendMessage(message, method, channelSequence);
        }


        /// <summary>
        /// Disconnects the connection.
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected)
            {
                _connection.Disconnect("");
            }
        }


        public void Dispose()
        {
            Disconnect();
            _cancellationTokenSource.Cancel();
        }
    }
}
