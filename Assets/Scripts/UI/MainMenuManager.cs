using System;
using Cysharp.Threading.Tasks;
using TMPro;
using TurnBasedStrategy.Network;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TurnBasedStrategy.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_InputField _ipInputField;
        [SerializeField] private GameObject _connectionPanel;
        [SerializeField] private TextMeshProUGUI _connectionStatusText;
        
        private void Start()
        {
            _hostButton?.onClick.AddListener(() => StartGame(true).Forget());
            _joinButton?.onClick.AddListener(() => StartGame(false).Forget());
            _quitButton?.onClick.AddListener(QuitGame);
            
            _connectionPanel?.SetActive(false);
            
            if (_ipInputField != null)
            {
                _ipInputField.text = "127.0.0.1";
            }
        }
        
        private async UniTaskVoid StartGame(bool asHost)
        {
            ShowConnectionPanel("Loading game scene...");
            await SceneManager.LoadSceneAsync("GameScene");
            
            await UniTask.NextFrame();
            
            var networkGameManager = FindObjectOfType<NetworkGameManager>();
            
            if (networkGameManager == null)
            {
                ShowConnectionPanel("Error: NetworkGameManager not found!");
                return;
            }
            
            if (!asHost && _ipInputField != null)
            {
                PlayerPrefs.SetString("ServerIP", _ipInputField.text);
            }
            
            ShowConnectionPanel(asHost ? "Starting host..." : "Connecting to host...");
            
            try
            {
                if (asHost)
                {
                    await networkGameManager.StartHost();
                    ShowConnectionPanel("Host started. Waiting for player...");
                }
                else
                {
                    await networkGameManager.StartClient();
                    ShowConnectionPanel("Connected to host!");
                }
            }
            catch (Exception e)
            {
                ShowConnectionPanel($"Connection failed: {e.Message}");
                await UniTask.Delay(2000);
                SceneManager.LoadScene("MainMenu");
            }
        }
        
        private void ShowConnectionPanel(string message)
        {
            if (_connectionPanel != null)
            {
                _connectionPanel.SetActive(true);
            }
            
            if (_connectionStatusText != null)
            {
                _connectionStatusText.text = message;
            }
        }
        
        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
