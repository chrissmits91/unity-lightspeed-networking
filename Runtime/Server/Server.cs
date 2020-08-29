using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Lightspeed.Shared;

namespace Lightspeed.Server
{
    public static class Server
    {
        public static int MaxPlayers { get; } = 50;
        private static int Port { get; } = 26950;
        
        public static readonly Dictionary<int, GameServerClient> Clients = new Dictionary<int, GameServerClient>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        private static int nextPacketHandlerId = 2;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public delegate void ServerStartDelegate();
        public static event ServerStartDelegate EventServerStart;

        public delegate void ServerStartedDelegate();
        public static event ServerStartedDelegate EventServerStarted;

        public static void Start()
        {
            Debug.Log("Starting server...");
            InitializeServerData();

            EventServerStart?.Invoke();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TcpConnectCallback, null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UdpReceiveCallback, null);

            EventServerStarted?.Invoke();

            Debug.Log($"Server started on port {Port}.");
        }

        private static void TcpConnectCallback(IAsyncResult _result)
        {
            var _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TcpConnectCallback, null);
            Debug.Log($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            for (var _i = 1; _i <= MaxPlayers; _i++)
            {
                if (Clients[_i].tcp.socket != null) continue;
                Clients[_i].tcp.Connect(_client);
                return;
            }

            Debug.Log($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void UdpReceiveCallback(IAsyncResult _result)
        {
            try
            {
                var _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UdpReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (var _packet = new Packet(_data))
                {
                    var _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        return;
                    }

                    if (Clients[_clientId].udp.endPoint == null)
                    {
                        Clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if (Clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        Clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error receiving UDP data: {_ex}");
            }
        }

        public static void SendUdpData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        private static void InitializeServerData()
        {
            for (var _i = 1; _i <= MaxPlayers; _i++)
            {
                Clients.Add(_i, new GameServerClient(_i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.WelcomeReceived, ServerHandle.WelcomeReceived }
            };

            Debug.Log("Initialized packets.");
        }

        public static void AddClientPacketHandler(PacketHandler _handler)
        {
            packetHandlers.Add(nextPacketHandlerId, _handler);
            nextPacketHandlerId++;
        }

        public static void Stop()
        {
            tcpListener.Stop();
            udpListener.Close();
        }
    }
}
