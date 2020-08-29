using System.Net;
using UnityEngine;
using Lightspeed.Shared;

namespace Lightspeed.Client
{
    public class ClientHandle : MonoBehaviour
    {
        public static void Welcome(Packet _packet)
        {
            var _msg = _packet.ReadString();
            var _myId = _packet.ReadInt();

            Debug.Log($"Message from server: {_msg}");
            GameClient.instance.connectionId = _myId;
            ClientSend.WelcomeReceived();

            GameClient.instance.udp.Connect(((IPEndPoint)GameClient.instance.tcp.socket.Client.LocalEndPoint).Port);

            GameManager.instance.JoinGame();
        }

        public static void SpawnPlayer(Packet _packet)
        {
            var _netId = _packet.ReadInt();
            var _username = _packet.ReadString();
            var _position = _packet.ReadVector3();
            var _rotation = _packet.ReadQuaternion();

            GameManager.instance.SpawnPlayer(_netId, _username, _position, _rotation);
        }

        public static void PlayerDisconnected(Packet _packet)
        {
            var _id = _packet.ReadInt();

            Destroy(GameManager.Players[_id].gameObject);
            GameManager.Players.Remove(_id);
        }
    }
}
