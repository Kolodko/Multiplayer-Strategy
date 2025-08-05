#if UNITY_EDITOR
using TurnBasedStrategy.Core;
using UnityEditor;
using UnityEngine;

namespace TurnBasedStrategy.Editor
{
    [CustomEditor(typeof(GameSettings))]
    public class GameSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _turnDuration;
        private SerializedProperty _drawTurnNumber;
        private SerializedProperty _slowRangedUnit;
        private SerializedProperty _fastMeleeUnit;
        private SerializedProperty _boardSize;
        private SerializedProperty _obstacles;
        private SerializedProperty _playerSpawnZones;
        
        private void OnEnable()
        {
            _turnDuration = serializedObject.FindProperty("TurnDuration");
            _drawTurnNumber = serializedObject.FindProperty("DrawTurnNumber");
            _slowRangedUnit = serializedObject.FindProperty("SlowRangedUnit");
            _fastMeleeUnit = serializedObject.FindProperty("FastMeleeUnit");
            _boardSize = serializedObject.FindProperty("BoardSize");
            _obstacles = serializedObject.FindProperty("Obstacles");
            _playerSpawnZones = serializedObject.FindProperty("PlayerSpawnZones");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Turn Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_turnDuration);
            EditorGUILayout.PropertyField(_drawTurnNumber);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unit Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_slowRangedUnit, true);
            EditorGUILayout.PropertyField(_fastMeleeUnit, true);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Board Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_boardSize);
            EditorGUILayout.PropertyField(_obstacles, true);
            EditorGUILayout.PropertyField(_playerSpawnZones, true);
            
            if (GUILayout.Button("Validate Settings"))
            {
                ValidateSettings();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void ValidateSettings()
        {
            var settings = target as GameSettings;
            
            if (settings.TurnDuration <= 0)
            {
                UnityEngine.Debug.LogWarning("Turn duration must be positive!");
            }
            
            if (settings.BoardSize.x <= 0 || settings.BoardSize.y <= 0)
            {
                UnityEngine.Debug.LogWarning("Board size must be positive!");
            }
            
            UnityEngine.Debug.Log("Settings validated!");
        }
    }
}
#endif
