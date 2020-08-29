using UnityEngine;
using Lightspeed.Shared;

namespace Lightspeed.Server
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager instance;

        public GameObject playerPrefab;

        private void Awake()
        {
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
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 100;

            Server.Start();
        }

        private void OnApplicationQuit()
        {
            Server.Stop();
        }

        public PlayerEntity InstantiatePlayer()
        {
            return Instantiate(playerPrefab, new Vector3(5f, 0.5f, -5f), Quaternion.identity).GetComponent<PlayerEntity>();
        }
    }
}
