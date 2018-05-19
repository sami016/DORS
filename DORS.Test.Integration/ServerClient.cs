using DORS.Servers;
using DORS.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ActionSerializer;
using Lidgren.Network;
using Xunit;
using FluentAssertions;

namespace DORS.Test.Integration
{
    public class ServerClient
    {
        [ActionType("A")]
        public class A
        {
            public string SomeData { get; } = "SomeData";
        }

        private static bool PerformApprovalCheck(RemoteClient remoteClient, object approvalAction)
        {
            return approvalAction is A;
        }

        private static readonly Random Random = new Random();
        private readonly int Server1Port = 12320 + Random.Next(1000);
        private readonly int Server2Port = 12320 + Random.Next(1000);

        private DorsClientConfiguration ClientConfiguration { get; }
        private DorsServerConfiguration Server1Configuration { get; }
        private DorsServerConfiguration Server2Configuration { get; }

        public ServerClient()
        {
            ActionSerialization.ActionConvert.SetActionAssemblies(typeof(ServerClient).Assembly);

            ClientConfiguration = new DorsClientConfiguration("test");
            Server1Configuration = new DorsServerConfiguration("test");
            Server1Configuration.EnableApproval(PerformApprovalCheck);
            Server1Configuration.LocalPort = Server1Port;
            Server2Configuration = new DorsServerConfiguration("test");
            Server2Configuration.EnableApproval(PerformApprovalCheck);
            Server2Configuration.LocalPort = Server2Port;
        }

        [Fact]
        public async Task BasicFlow()
        {

            var server = new ServerControl(Server1Configuration);
            server.Start();

            var client = new ClientControl(ClientConfiguration);
            using (var serverMonitor = server.Monitor())
            using (var clientMonitor = client.Monitor())
            {
                var success = await client.AsyncConnect("localhost", Server1Port, new A());
                Thread.Sleep(50);
                serverMonitor.Should().Raise(nameof(ServerControl.Connected));
                clientMonitor.Should().Raise(nameof(ClientControl.Connected));
                success.Should().BeTrue();
            }

            // Send message from client to server.
            using (var serverMonitor = server.Monitor())
            {
                client.Send(new A());
                Thread.Sleep(50);
                serverMonitor.Should().Raise(nameof(ServerControl.MessageReceived));
            }

            // Send message from server to the client.
            using (var clientMonitor = client.Monitor())
            {
                server.Send(server.NetServer.Connections.First(), new A());
                Thread.Sleep(50);
                clientMonitor.Should().Raise(nameof(ClientControl.MessageReceived));
            }

            // Broadcast message from server to the client.
            using (var clientMonitor = client.Monitor())
            {
                server.Send(new A());
                Thread.Sleep(50);
                clientMonitor.Should().Raise(nameof(ClientControl.MessageReceived));
            }

            // Disconnect.
            using (var serverMonitor = server.Monitor())
            using (var clientMonitor = client.Monitor())
            {
                client.Disconnect();
                Thread.Sleep(50);
                serverMonitor.Should().Raise(nameof(ServerControl.Disconnected));
                clientMonitor.Should().Raise(nameof(ClientControl.Disconnected));
            }
            server.NetServer.ConnectionsCount.Should().Be(0);
        }



        [Fact]
        public async Task ServerDisconnectFlow()
        {

            var server = new ServerControl(Server1Configuration);
            server.Start();

            var clientConfig = new NetPeerConfiguration("test");

            var client = new ClientControl(ClientConfiguration);
            var success = await client.AsyncConnect("localhost", Server1Port, new A());
            success.Should().BeTrue();

            // Disconnect.
            using (var serverMonitor = server.Monitor())
            using (var clientMonitor = client.Monitor())
            {
                server.NetServer.Connections.First().Disconnect("");
                Thread.Sleep(50);
                serverMonitor.Should().Raise(nameof(ServerControl.Disconnected));
                clientMonitor.Should().Raise(nameof(ClientControl.Disconnected));
            }
            server.NetServer.ConnectionsCount.Should().Be(0);
        }




        [Fact]
        public async Task HopFlow()
        {
            ActionSerialization.ActionConvert.SetActionAssemblies(typeof(ServerClient).Assembly);

            var server = new ServerControl(Server1Configuration);
            server.Start();
            var server2 = new ServerControl(Server2Configuration);
            server2.Start();

            var clientConfig = new NetPeerConfiguration("test");
            var client = new ClientControl(ClientConfiguration);

            // Connect client to server 1.    
            var success = await client.AsyncConnect("localhost", Server1Port, new A());
            Thread.Sleep(50);
            server.NetServer.ConnectionsCount.Should().Be(1);
            server2.NetServer.ConnectionsCount.Should().Be(0);
            success.Should().BeTrue();


            // Hop to server 2.
            using (var serverMonitor = server.Monitor())
            using (var server2Monitor = server2.Monitor())
            using (var clientMonitor = client.Monitor())
            {
                client.Disconnected += (a, b) =>
                {

                };
                var firstHopSuccess = client.WaitHop("localhost", Server2Port, new A());
                Thread.Sleep(50);
                server.NetServer.ConnectionsCount.Should().Be(0);
                server2.NetServer.ConnectionsCount.Should().Be(1);
                firstHopSuccess.Should().BeTrue();

                clientMonitor.Should().Raise(nameof(ClientControl.Hopped));
                clientMonitor.Should().NotRaise(nameof(ClientControl.Connected));
                clientMonitor.Should().NotRaise(nameof(ClientControl.Disconnected));
                serverMonitor.Should().Raise(nameof(ServerControl.Disconnected));
                server2Monitor.Should().Raise(nameof(ServerControl.Connected));
            }

            // Hop back to server 1.
            var secondHopSuccess = client.WaitHop("localhost", Server1Port, new A());
            Thread.Sleep(50);
            server.NetServer.ConnectionsCount.Should().Be(1);
            server2.NetServer.ConnectionsCount.Should().Be(0);
            secondHopSuccess.Should().BeTrue();

        }
    }
}
