using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Lightspeed.Shared;

namespace Lightspeed.Server
{
    public class GameServerClient
    {
        private static int DataBufferSize { get; } = 4096;
        private readonly int id;
        public PlayerEntity player;
        public readonly Tcp tcp;
        public readonly Udp udp;

        public GameServerClient(int _clientId)
        {
            id = _clientId;
            tcp = new Tcp(id);
            udp = new Udp(id);
        }

        public class Tcp
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public Tcp(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = DataBufferSize;
                socket.SendBufferSize = DataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[DataBufferSize];

                stream.BeginRead(receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception _ex)
                {
                    Debug.Log($"Error sending data to player {id} via TCP: {_ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    var _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        Server.Clients[id].Disconnect();
                        return;
                    }

                    var _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Debug.Log($"Error receiving TCP data: {_ex}");
                    Server.Clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                var _packetLength = 0;

                receivedData.SetBytes(_data);

                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    var _packetBytes = receivedData.ReadBytes(_packetLength);
                    ServerThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (var _packet = new Packet(_packetBytes))
                        {
                            var _packetId = _packet.ReadInt();
                            Server.packetHandlers[_packetId](id, _packet);
                        }
                    });

                    _packetLength = 0;
                    if (receivedData.UnreadLength() < 4) continue;
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }

                return _packetLength <= 1;
            }

            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class Udp
        {
            public IPEndPoint endPoint;

            private readonly int id;

            public Udp(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendData(Packet _packet)
            {
                Server.SendUdpData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                var _packetLength = _packetData.ReadInt();
                var _packetBytes = _packetData.ReadBytes(_packetLength);

                ServerThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var _packet = new Packet(_packetBytes))
                    {
                        var _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });
            }

            public void Disconnect()
            {
                endPoint = null;
            }
        }

        public void SendIntoGame(string _username)
        {
            player = NetworkManager.instance.InstantiatePlayer();
            player.Initialize(id, _username);

            // Send all players to the new player
            foreach (var _client in Server.Clients.Values.Where(_client => _client.player != null).Where(_client => _client.id != id))
            {
                ServerSend.SpawnPlayer(id, _client.player);
            }

            // Send the new player to all players (including himself)
            foreach (var _client in Server.Clients.Values.Where(_client => _client.player != null))
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        private void Disconnect()
        {
            Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

            ServerThreadManager.ExecuteOnMainThread(() => 
            { 
                UnityEngine.Object.Destroy(player.gameObject);
                player = null;
            });

            tcp.Disconnect();
            udp.Disconnect();

            ServerSend.PlayerDisconnected(id);
        }
    }
}
