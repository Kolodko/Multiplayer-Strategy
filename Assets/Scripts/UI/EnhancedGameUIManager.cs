using System.Collections;
using TMPro;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Gameplay;
using TurnBasedStrategy.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TurnBasedStrategy.UI
{
    public class EnhancedGameUIManager : MonoBehaviour
    {
        [Header("Turn Information")]
        [SerializeField] private TextMeshProUGUI _turnTimerText;
        [SerializeField] private TextMeshProUGUI _turnNumberText;
        [SerializeField] private TextMeshProUGUI _currentPlayerText;
        [SerializeField] private Button _endTurnButton;
        
        [Header("Action Counters")]
        [SerializeField] private GameObject _actionPanel;
        [SerializeField] private TextMeshProUGUI _moveCountText;
        [SerializeField] private TextMeshProUGUI _attackCountText;
        [SerializeField] private Image _moveIcon;
        [SerializeField] private Image _attackIcon;
        [SerializeField] private Color _availableActionColor = Color.green;
        [SerializeField] private Color _usedActionColor = Color.gray;
        
        [Header("Turn Counter")]
        [SerializeField] private TextMeshProUGUI _turnCounterText;
        [SerializeField] private GameObject _turnCounterPanel;
        
        [Header("Game Over Panel")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private Button _returnToMenuButton;
        
        [Header("Visual Settings")]
        [SerializeField] private AnimationCurve _timerPulseCurve;
        [SerializeField] private float _criticalTimeThreshold = 10f;
        
        private ITurnManager _turnManager;
        private INetworkGameManager _networkManager;
        private float _originalTimerScale;
        private Color _originalTimerColor;
        
        private void Start()
        {
            _turnManager = FindObjectOfType<NetworkTurnManager>();
            _networkManager = FindObjectOfType<NetworkGameManager>();
            
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _turnManager.OnTimerUpdated += OnTimerUpdated;
                _turnManager.OnGameEnded += OnGameEnded;
            }
            
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
            
            if (_returnToMenuButton != null)
            {
                _returnToMenuButton.onClick.AddListener(ReturnToMenu);
            }
            
            if (_turnTimerText != null)
            {
                _originalTimerScale = _turnTimerText.transform.localScale.x;
                _originalTimerColor = _turnTimerText.color;
            }
            
            _gameOverPanel?.SetActive(false);
            
            StartCoroutine(InitializeUIAfterDelay());
        }
        
        private IEnumerator InitializeUIAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (_turnManager != null)
            {
                UpdateTurnDisplay(_turnManager.CurrentPlayerId);
                UpdateActionCounters();
                OnTimerUpdated(_turnManager.RemainingTime);
            }
        }
        
        private void Update()
        {
            UpdateActionCounters();
            AnimateTimerIfCritical();
            
            if (_turnManager != null && _turnTimerText != null)
            {
                OnTimerUpdated(_turnManager.RemainingTime);
            }
        }
        
        private void OnTimerUpdated(float remainingTime)
        {
            if (_turnTimerText != null)
            {
                int seconds = Mathf.CeilToInt(remainingTime);
                _turnTimerText.text = $"{seconds:00}";
                
                if (remainingTime <= _criticalTimeThreshold)
                {
                    float t = (_criticalTimeThreshold - remainingTime) / _criticalTimeThreshold;
                    _turnTimerText.color = Color.Lerp(_originalTimerColor, Color.red, t);
                }
                else
                {
                    _turnTimerText.color = _originalTimerColor;
                }
            }
        }
        
        private void AnimateTimerIfCritical()
        {
            if (_turnTimerText != null && _turnManager != null && _turnManager.RemainingTime <= _criticalTimeThreshold)
            {
                float normalizedTime = (_criticalTimeThreshold - _turnManager.RemainingTime) / _criticalTimeThreshold;
                float scale = _originalTimerScale * (1f + _timerPulseCurve.Evaluate(Time.time % 1f) * 0.2f * normalizedTime);
                _turnTimerText.transform.localScale = Vector3.one * scale;
            }
            else if (_turnTimerText != null)
            {
                _turnTimerText.transform.localScale = Vector3.one * _originalTimerScale;
            }
        }
        
        private void OnTurnChanged(int currentPlayerId)
        {
            UpdateTurnDisplay(currentPlayerId);
            UpdateActionCounters();
        }
        
        private void UpdateTurnDisplay(int currentPlayerId)
        {
            if (_currentPlayerText != null)
            {
                _currentPlayerText.text = $"Player {currentPlayerId + 1}";
                
                if (_networkManager != null && currentPlayerId == _networkManager.LocalPlayerId)
                {
                    _currentPlayerText.color = _availableActionColor;
                    _currentPlayerText.text += " (You)";
                }
                else
                {
                    _currentPlayerText.color = Color.white;
                }
            }
            
            if (_turnNumberText != null)
            {
                _turnNumberText.text = $"Turn {_turnManager.TurnNumber}";
            }
            
            if (_endTurnButton != null)
            {
                bool isMyTurn = _networkManager != null && currentPlayerId == _networkManager.LocalPlayerId;
                _endTurnButton.interactable = isMyTurn;
                _endTurnButton.GetComponentInChildren<TextMeshProUGUI>().text = isMyTurn ? "End Turn" : "Waiting...";
            }
            
            UpdateTurnCounter();
        }
        
        private void UpdateActionCounters()
        {
            if (_turnManager == null || _networkManager == null) return;
            
            bool isMyTurn = _turnManager.CurrentPlayerId == _networkManager.LocalPlayerId;
            
            if (_actionPanel != null)
            {
                _actionPanel.SetActive(isMyTurn);
            }
            
            if (isMyTurn)
            {
                if (_moveCountText != null)
                {
                    _moveCountText.text = _turnManager.CanMove ? "1" : "0";
                    _moveCountText.color = _turnManager.CanMove ? _availableActionColor : _usedActionColor;
                }
                
                if (_attackCountText != null)
                {
                    _attackCountText.text = _turnManager.CanAttack ? "1" : "0";
                    _attackCountText.color = _turnManager.CanAttack ? _availableActionColor : _usedActionColor;
                }
                
                if (_moveIcon != null)
                {
                    _moveIcon.color = _turnManager.CanMove ? _availableActionColor : _usedActionColor;
                }
                
                if (_attackIcon != null)
                {
                    _attackIcon.color = _turnManager.CanAttack ? _availableActionColor : _usedActionColor;
                }
            }
        }
        
        private void UpdateTurnCounter()
        {
            if (_turnCounterText != null)
            {
                _turnCounterText.text = $"Turn {_turnManager.TurnNumber}";
                
                if (_turnManager.TurnNumber >= 10)
                {
                    _turnCounterText.color = Color.yellow;
                }
                
                if (_turnManager.TurnNumber >= 15)
                {
                    _turnCounterText.color = Color.red;
                    _turnCounterText.text += " (Draw Conditions Active)";
                }
            }
        }
        
        private void OnEndTurnClicked()
        {
            if (_turnManager != null && _networkManager != null)
            {
                var turnManager = _turnManager as NetworkTurnManager;
                turnManager?.RequestEndTurnServerRpc();
            }
        }
        
        private void OnGameEnded()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                
                var unitManager = FindObjectOfType<UnitManager>();
                var player0Units = unitManager.GetPlayerUnits(0).Count;
                var player1Units = unitManager.GetPlayerUnits(1).Count;
                
                string winnerMessage;
                
                if (player0Units > player1Units)
                {
                    winnerMessage = "Player 1 Wins!";
                    _winnerText.color = _networkManager.LocalPlayerId == 0 ? _availableActionColor : Color.red;
                }
                else if (player1Units > player0Units)
                {
                    winnerMessage = "Player 2 Wins!";
                    _winnerText.color = _networkManager.LocalPlayerId == 1 ? _availableActionColor : Color.red;
                }
                else
                {
                    winnerMessage = "Draw!";
                    _winnerText.color = Color.yellow;
                }
                
                if (_winnerText != null)
                {
                    _winnerText.text = winnerMessage;
                }
            }
        }
        
        private void ReturnToMenu()
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }
        
        private void OnDestroy()
        {
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _turnManager.OnTimerUpdated -= OnTimerUpdated;
                _turnManager.OnGameEnded -= OnGameEnded;
            }
        }
    }
}
