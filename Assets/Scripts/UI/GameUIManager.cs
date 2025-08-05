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
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _turnTimerText;
        [SerializeField] private TextMeshProUGUI _turnNumberText;
        [SerializeField] private TextMeshProUGUI _currentPlayerText;
        [SerializeField] private Button _endTurnButton;
        
        [Header("Action Indicators")]
        [SerializeField] private GameObject _moveIndicator;
        [SerializeField] private GameObject _attackIndicator;
        [SerializeField] private Image _moveIndicatorFill;
        [SerializeField] private Image _attackIndicatorFill;
        
        [Header("Game Over Panel")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private Button _returnToMenuButton;
        
        private ITurnManager _turnManager;
        private INetworkGameManager _networkManager;
        
        private void Start()
        {
            _turnManager = FindObjectOfType<NetworkTurnManager>();
            _networkManager = FindObjectOfType<NetworkGameManager>();
            
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += UpdateTurnDisplay;
                _turnManager.OnTimerUpdated += UpdateTimer;
                _turnManager.OnGameEnded += ShowGameOver;
            }
            
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
            
            if (_returnToMenuButton != null)
            {
                _returnToMenuButton.onClick.AddListener(ReturnToMenu);
            }
            
            _gameOverPanel?.SetActive(false);
        }
        
        private void Update()
        {
            UpdateActionIndicators();
        }
        
        private void UpdateTimer(float remainingTime)
        {
            if (_turnTimerText != null)
            {
                int seconds = Mathf.CeilToInt(remainingTime);
                _turnTimerText.text = $"Time: {seconds:00}";
                
                if (remainingTime < 10f)
                {
                    _turnTimerText.color = Color.red;
                }
                else
                {
                    _turnTimerText.color = Color.white;
                }
            }
        }
        
        private void UpdateTurnDisplay(int currentPlayerId)
        {
            if (_currentPlayerText != null)
            {
                _currentPlayerText.text = $"Player {currentPlayerId + 1}'s Turn";
                
                if (_networkManager != null && currentPlayerId == _networkManager.LocalPlayerId)
                {
                    _currentPlayerText.color = Color.green;
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
                _endTurnButton.interactable = _networkManager != null && 
                                            currentPlayerId == _networkManager.LocalPlayerId;
            }
        }
        
        private void UpdateActionIndicators()
        {
            if (_turnManager == null || _networkManager == null)
                return;
            
            bool isMyTurn = _turnManager.CurrentPlayerId == _networkManager.LocalPlayerId;
            
            if (_moveIndicator != null)
            {
                _moveIndicator.SetActive(isMyTurn);
                
                if (_moveIndicatorFill != null)
                {
                    _moveIndicatorFill.fillAmount = _turnManager.CanMove ? 1f : 0f;
                    _moveIndicatorFill.color = _turnManager.CanMove ? Color.green : Color.gray;
                }
            }
            
            if (_attackIndicator != null)
            {
                _attackIndicator.SetActive(isMyTurn);
                
                if (_attackIndicatorFill != null)
                {
                    _attackIndicatorFill.fillAmount = _turnManager.CanAttack ? 1f : 0f;
                    _attackIndicatorFill.color = _turnManager.CanAttack ? Color.red : Color.gray;
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
        
        private void ShowGameOver()
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
                }
                else if (player1Units > player0Units)
                {
                    winnerMessage = "Player 2 Wins!";
                }
                else
                {
                    winnerMessage = "Draw!";
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
                _turnManager.OnTurnChanged -= UpdateTurnDisplay;
                _turnManager.OnTimerUpdated -= UpdateTimer;
                _turnManager.OnGameEnded -= ShowGameOver;
            }
        }
    }
}
