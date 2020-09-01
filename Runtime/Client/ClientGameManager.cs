using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Lightspeed.Shared;

namespace Lightspeed.Client
{
    public class ClientGameManager : MonoBehaviour
    {
        public static ClientGameManager instance;

        public static readonly Dictionary<int, PlayerEntity> Players = new Dictionary<int, PlayerEntity>();

        [SerializeField] private GameObject localPlayerPrefab;

        [SerializeField] private GameObject playerPrefab;

        public delegate void JoinGameDelegate();
        public event JoinGameDelegate EventGameJoined;

        public delegate void LocalPlayerSpawnedDelegate(GameObject _player);
        public event LocalPlayerSpawnedDelegate EventLocalPlayerSpawned;

        public delegate void PlayerSpawnedDelegate(GameObject _player);
        public event PlayerSpawnedDelegate EventPlayerSpawned;

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

        public void JoinGame()
        {
            EventGameJoined?.Invoke();
        }

        public void SpawnPlayer(int _netId, string _username, Vector3 _position, Quaternion _rotation)
        {
            GameObject _player;

            if (_netId == GameClient.instance.connectionId)
            {
                _player = Instantiate(localPlayerPrefab, _position, _rotation);
            }
            else
            {
                _player = Instantiate(playerPrefab, _position, _rotation);
            }

            _player.GetComponent<PlayerEntity>().Initialize(_netId, _username);
            Players.Add(_netId, _player.GetComponent<PlayerEntity>());

            if (_netId == GameClient.instance.connectionId)
            {
                EventLocalPlayerSpawned?.Invoke(_player);
            }
            else
            {
                EventPlayerSpawned?.Invoke(_player);
            }
        }
    }
}
