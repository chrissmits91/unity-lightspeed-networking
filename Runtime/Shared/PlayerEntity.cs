using UnityEngine;

namespace Lightspeed.Shared 
{
    public class PlayerEntity : NetworkEntity
    {
        public string username;

        public void Initialize(int _netId, string _username)
        {
            netId = _netId;
            username = _username;
        }
    }
}