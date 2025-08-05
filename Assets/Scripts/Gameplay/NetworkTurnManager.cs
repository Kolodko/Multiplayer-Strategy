using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Units;
using Unity.Netcode;
using UnityEngine;

namespace TurnBasedStrategy.Gameplay
{
    public class NetworkTurnManager : NetworkBehaviour, ITurnManager
    {
        [Header("Turn Settings")]
        [SerializeField] private GameSettings _gameSettings;
        
        private NetworkVariable<int> _currentPlayerId = new NetworkVariable<int>(0);
        private NetworkVariable<int> _turnNumber = new NetworkVariable<int>(1);
        private NetworkVariable<float> _turnTimer = new NetworkVariable<float>(60f);
        private NetworkVariable<bool> _gameActive = new NetworkVariable<bool>(false);
        
        private Dictionary<int, HashSet<ActionType>> _playerActions = new Dictionary<int, HashSet<ActionType>>();
        
        public int CurrentPlayerId => _currentPlayerId.Value;
        public int TurnNumber => _turnNumber.Value;
        public float RemainingTime => _turnTimer.Value;
        public bool CanMove => !_playerActions.ContainsKey(CurrentPlayerId) || 
                               !_playerActions[CurrentPlayerId].Contains(ActionType.Move);
        public bool CanAttack => !_playerActions.ContainsKey(CurrentPlayerId) || 
                                !_playerActions[CurrentPlayerId].Contains(ActionType.Attack);
        
        public event Action<int> OnTurnChanged;
        public event Action<float> OnTimerUpdated;
        public event Action OnGameEnded;
        
        private void Awake()
        {
            if (_gameSettings == null)
            {
                _gameSettings = Resources.Load<GameSettings>("GameSettings");
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            _currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
            _turnNumber.OnValueChanged += OnTurnNumberChanged;
            _turnTimer.OnValueChanged += OnTimerChanged;
            
            if (IsServer)
            {
                _playerActions[0] = new HashSet<ActionType>();
                _playerActions[1] = new HashSet<ActionType>();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            _currentPlayerId.OnValueChanged -= OnCurrentPlayerChanged;
            _turnNumber.OnValueChanged -= OnTurnNumberChanged;
            _turnTimer.OnValueChanged -= OnTimerChanged;
            
            base.OnNetworkDespawn();
        }
        
        public void StartGame()
        {
            if (!IsServer)
                return;
            
            _gameActive.Value = true;
            _turnTimer.Value = _gameSettings.TurnDuration;
            StartTurnTimer().Forget();
        }
        
        public async UniTask EndTurn()
        {
            if (!IsServer)
                return;
            
            await UniTask.Yield();
            
            if (_turnNumber.Value >= _gameSettings.DrawTurnNumber)
            {
                CheckDrawConditions();
            }
            
            _currentPlayerId.Value = (_currentPlayerId.Value + 1) % 2;
            _turnNumber.Value++;
            
            _playerActions[_currentPlayerId.Value].Clear();
            
            _turnTimer.Value = _gameSettings.TurnDuration;
            
            EndTurnClientRpc();
        }
        
        public void RegisterAction(ActionType type)
        {
            if (!IsServer)
                return;
            
            if (!_playerActions.ContainsKey(_currentPlayerId.Value))
            {
                _playerActions[_currentPlayerId.Value] = new HashSet<ActionType>();
            }
            
            _playerActions[_currentPlayerId.Value].Add(type);
            
            if (_playerActions[_currentPlayerId.Value].Count >= 2)
            {
                EndTurn().Forget();
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if ((int)clientId != _currentPlayerId.Value)
                return;
            
            EndTurn().Forget();
        }
        
        [ClientRpc]
        private void EndTurnClientRpc()
        {
            //TODO
        }
        
        private async UniTaskVoid StartTurnTimer()
        {
            while (_gameActive.Value)
            {
                await UniTask.Delay(100);
                
                if (_turnTimer.Value > 0)
                {
                    _turnTimer.Value -= 0.1f;
                    
                    if (_turnTimer.Value <= 0)
                    {
                        await EndTurn();
                    }
                }
            }
        }
        
        private void CheckDrawConditions()
        {
            var unitManager = FindObjectOfType<UnitManager>();
            var player0Units = unitManager.GetPlayerUnits(0).Count;
            var player1Units = unitManager.GetPlayerUnits(1).Count;
            
            if (player0Units != player1Units)
            {
                var winner = player0Units > player1Units ? 0 : 1;
                EndGame(winner);
            }
            else
            {
                EnableInfiniteMovement();
            }
        }
        
        private void EnableInfiniteMovement()
        {
            if (!IsServer) return;
            
            EnableInfiniteMovementClientRpc();
        }
        
        [ClientRpc]
        private void EnableInfiniteMovementClientRpc()
        {
            var allUnits = FindObjectsOfType<NetworkUnit>();
            
            foreach (var unit in allUnits)
            {
                //TODO
            }
        }
        
        private void EndGame(int winnerId)
        {
            if (!IsServer) 
                return;
            
            _gameActive.Value = false;
            EndGameClientRpc(winnerId);
            OnGameEnded?.Invoke();
        }
        
        [ClientRpc]
        private void EndGameClientRpc(int winnerId)
        {
            //TODO
        }
        
        private void OnCurrentPlayerChanged(int oldValue, int newValue)
        {
            OnTurnChanged?.Invoke(newValue);
        }
        
        private void OnTurnNumberChanged(int oldValue, int newValue)
        {
            //TODO
        }
        
        private void OnTimerChanged(float oldValue, float newValue)
        {
            OnTimerUpdated?.Invoke(newValue);
        }
    }
}

