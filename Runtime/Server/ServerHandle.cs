using UnityEngine;
using Lightspeed.Shared;

namespace Lightspeed.Server
{
    public static class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            var _clientIdCheck = _packet.ReadInt();
            var _username = _packet.ReadString();

            Debug.Log($"{Server.Clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != _clientIdCheck)
            {
                Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
            Server.Clients[_fromClient].SendIntoGame(_username);
        }
    }
}
