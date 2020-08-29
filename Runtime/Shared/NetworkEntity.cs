using UnityEngine;

namespace Lightspeed.Shared 
{
    public enum SyncType
    {
        Owner = 1,
        Observers
    }

    public class NetworkEntity : MonoBehaviour 
    {
        public int netId;

        public SyncType syncType = SyncType.Observers;

        public float syncInterval = 0.1f;
    }
}