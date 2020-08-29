using System.Collections.Generic;
using Lightspeed.Shared;

namespace Lightspeed.Server
{
    public static class ServerSend
    {
        public static Dictionary<string, int> customServerSendActions = new Dictionary<string, int>();
        private static int nextServerSendId = 4;

        public static void RegisterServerSendAction(string _action)
        {
            customServerSendActions.Add(_action, nextServerSendId);
            nextServerSendId++;
        }

        public static void SendTcpData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.Clients[_toClient].tcp.SendData(_packet);
        }

        public static void SendUdpData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.Clients[_toClient].udp.SendData(_packet);
        }

        public static void SendTcpDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (var _i = 1; _i <= Server.MaxPlayers; _i++)
            {
                Server.Clients[_i].tcp.SendData(_packet);
            }
        }
        public static void SendTcpDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (var _i = 1; _i <= Server.MaxPlayers; _i++)
            {
                if (_i != _exceptClient)
                {
                    Server.Clients[_i].tcp.SendData(_packet);
                }
            }
        }

        public static void SendUdpDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (var _i = 1; _i <= Server.MaxPlayers; _i++)
            {
                Server.Clients[_i].udp.SendData(_packet);
            }
        }
        public static void SendUdpDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (var _i = 1; _i <= Server.MaxPlayers; _i++)
            {
                if (_i != _exceptClient)
                {
                    Server.Clients[_i].udp.SendData(_packet);
                }
            }
        }

        #region Packets
        public static void Welcome(int _toClient, string _msg)
        {
            using (var _packet = new Packet((int)ServerPackets.Welcome))
            {
                _packet.Write(_msg);
                _packet.Write(_toClient);

                SendTcpData(_toClient, _packet);
            }
        }

        public static void SpawnPlayer(int _toClient, PlayerEntity _player)
        {
            using (var _packet = new Packet((int)ServerPackets.SpawnPlayer))
            {
                _packet.Write(_player.netId);
                _packet.Write(_player.username);
                _packet.Write(_player.transform.position);
                _packet.Write(_player.transform.rotation);

                SendTcpData(_toClient, _packet);
            }
        }

        public static void PlayerDisconnected(int _netId)
        {
            using (var _packet = new Packet((int)ServerPackets.PlayerDisconnected))
            {
                _packet.Write(_netId);

                SendTcpDataToAll(_packet);
            }
        }

        #endregion
    }
}
