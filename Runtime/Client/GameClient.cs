using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Lightspeed.Shared;

namespace Lightspeed.Client
{
    public class GameClient : MonoBehaviour
    {
        public static GameClient instance;
        private static int DataBufferSize { get; } = 4096;
        public string ip = "127.0.0.1";
        public int port = 26950;
        public int connectionId;
        public string username = "Unknown";
        public Tcp tcp;
        public Udp udp;

        private bool isConnected;
        public delegate void PacketHandler(Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        private static int nextPacketHandlerId = 4;

        public delegate void ClientStartDelegate();
        public event ClientStartDelegate EventClientStart;

        public delegate void ClientStartedDelegate();
        public event ClientStartedDelegate EventClientStarted;

        private void Awake()
        {
            DontDestroyOnLoad(transform.gameObject);

            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.Log("Instance already exists, destroying object!");
                Destroy(this);
            }
        }

        private void Start()
        {
            tcp = new Tcp();
            udp = new Udp();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        public void ConnectToServer()
        {
            InitializeClientData();

            EventClientStart?.Invoke();

            isConnected = true;
            tcp.Connect();
        }

        public class Tcp
        {
            public TcpClient socket;

            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = DataBufferSize,
                    SendBufferSize = DataBufferSize
                };

                receiveBuffer = new byte[DataBufferSize];
                socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);

                instance.EventClientStarted?.Invoke();
            }

            private void ConnectCallback(IAsyncResult _result)
            {
                try
                {
                    socket.EndConnect(_result);
                }
                catch (Exception _ex)
                {
                    Debug.Log(_ex.Message);
                }

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
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
                    Debug.Log($"Error sending data to server via TCP: {_ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        instance.Disconnect();
                        return;
                    }

                    var _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch
                {
                    Disconnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;

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
                    ClientThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (var _packet = new Packet(_packetBytes))
                        {
                            var _packetId = _packet.ReadInt();
                            packetHandlers[_packetId](_packet);
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

            private void Disconnect()
            {
                instance.Disconnect();

                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class Udp
        {
            public UdpClient socket;
            private IPEndPoint endPoint;

            public Udp()
            {
                endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
            }

            public void Connect(int _localPort)
            {
                socket = new UdpClient(_localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                using (var _packet = new Packet())
                {
                    SendData(_packet);
                }
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    _packet.InsertInt(instance.connectionId);
                    socket?.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
                catch (Exception _ex)
                {
                    Debug.Log($"Error sending data to server via UDP: {_ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    var _data = socket.EndReceive(_result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);

                    if (_data.Length < 4)
                    {
                        instance.Disconnect();
                        return;
                    }

                    HandleData(_data);
                }
                catch
                {
                    Disconnect();
                }
            }

            private static void HandleData(byte[] _data)
            {
                using (var _packet = new Packet(_data))
                {
                    var _packetLength = _packet.ReadInt();
                    _data = _packet.ReadBytes(_packetLength);
                }

                ClientThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var _packet = new Packet(_data))
                    {
                        var _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
                    }
                });
            }

            private void Disconnect()
            {
                instance.Disconnect();

                endPoint = null;
                socket = null;
            }
        }
    
        private static void InitializeClientData()
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ServerPackets.Welcome, ClientHandle.Welcome },
                { (int)ServerPackets.SpawnPlayer, ClientHandle.SpawnPlayer },
                { (int)ServerPackets.PlayerDisconnected, ClientHandle.PlayerDisconnected }
            };

            Debug.Log("Initialized packets.");
        }

        public static void AddServerPacketHandler(PacketHandler _handler)
        {
            packetHandlers.Add(nextPacketHandlerId, _handler);
            nextPacketHandlerId++;
        }

        private void Disconnect()
        {
            if (!isConnected) return;
            
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected from server.");
        }
    }
}
