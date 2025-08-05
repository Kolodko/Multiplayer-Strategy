using Cysharp.Threading.Tasks;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Gameplay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TurnBasedStrategy.Network
{
    public class NetworkGameManager : MonoBehaviour, INetworkGameManager
    {
        [Header("Network Settings")]
        [SerializeField] private string _ipAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;
        
        private NetworkManager _networkManager;
        private NetworkTurnManager _turnManager;
        private UnitManager _unitManager;
        private GameBoard _gameBoard;
        
        public bool IsHost => _networkManager != null && _networkManager.IsHost;
        public int LocalPlayerId => _networkManager != null ? (int)_networkManager.LocalClientId : -1;
        
        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            _turnManager = GetComponent<NetworkTurnManager>();
            _unitManager = GetComponent<UnitManager>();
            _gameBoard = GetComponent<GameBoard>();
        }
        
        private void Start()
        {
            if (_networkManager == null)
            {
                _networkManager = NetworkManager.Singleton;
            }
            
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        public async UniTask StartHost()
        {
            ConfigureTransport();
            
            if (_networkManager.StartHost())
            {
                Debug.Log("Host started successfully");
                await UniTask.NextFrame();
            }
        }
        
        public async UniTask StartClient()
        {
            ConfigureTransport();
            
            if (_networkManager.StartClient())
            {
                Debug.Log("Client started successfully");
                await UniTask.NextFrame();
            }
        }
        
        private void ConfigureTransport()
        {
            var transport = _networkManager.GetComponent<UnityTransport>();
            
            if (transport != null)
            {
                transport.SetConnectionData(_ipAddress, _port);
            }
        }
        
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected");
            
            if (IsHost && _networkManager.ConnectedClientsList.Count == 2)
            {
                StartGame();
            }
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected");
        }
        
        private void StartGame()
        {
            if (!IsHost) 
                return;
            
            Debug.Log("Starting game with 2 players");
            
            _turnManager?.StartGame();
        }
        
        public void SendGameAction(GameAction action)
        {
            switch (action.Type)
            {
                case ActionType.Move:
                    HandleMoveAction(action);
                    break;
                case ActionType.Attack:
                    HandleAttackAction(action);
                    break;
            }
        }
        
        private void HandleMoveAction(GameAction action)
        {
            var unit = _unitManager.GetUnit((ulong)action.UnitId);
            
            if (unit != null)
            {
                unit.RequestMoveServerRpc(action.TargetPosition);
            }
        }
        
        private void HandleAttackAction(GameAction action)
        {
            var unit = _unitManager.GetUnit((ulong)action.UnitId);
            
            if (unit != null)
            {
                unit.RequestAttackServerRpc((ulong)action.TargetUnitId);
            }
        }
        
        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= OnClientConnected;
                _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
    }
}
