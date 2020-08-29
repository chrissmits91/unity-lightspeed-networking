using System.Collections.Generic;
using Lightspeed.Shared;

namespace Lightspeed.Client
{
    public static class ClientSend
    {
        public static Dictionary<string, int> customClientSendActions = new Dictionary<string, int>();
        private static int nextClientSendId = 2;

        public static void RegisterClientSendAction(string _action)
        {
            customClientSendActions.Add(_action, nextClientSendId);
            nextClientSendId++;
        }

        public static void SendTcpData(Packet _packet)
        {
            _packet.WriteLength();
            GameClient.instance.tcp.SendData(_packet);
        }

        public static void SendUdpData(Packet _packet)
        {
            _packet.WriteLength();
            GameClient.instance.udp.SendData(_packet);
        }

        public static void WelcomeReceived()
        {
            using (var _packet = new Packet((int)ClientPackets.WelcomeReceived))
            {
                _packet.Write(GameClient.instance.connectionId);
                _packet.Write(GameClient.instance.username);

                SendTcpData(_packet);
            }
        }
    }
}
