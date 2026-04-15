using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NexusFrame
{
    [CustomEditor(typeof(GamePlaySystem))]
    public class GamePlaySystemEditor : Editor
    {
        private static readonly Color ColorExploration = new(0.3f, 0.8f, 0.3f, 1f);
        private static readonly Color ColorBattle      = new(0.9f, 0.3f, 0.3f, 1f);
        private static readonly Color ColorNarrative   = new(0.4f, 0.6f, 1.0f, 1f);
        private static readonly Color ColorUnknown     = new(0.6f, 0.6f, 0.6f, 1f);

        private GamePlaySystem _target;

        private void OnEnable()
        {
            _target = (GamePlaySystem)target;
            if (Application.isPlaying)
            {
                _target.SetSessionStackUpdateCallback(Repaint);
            }
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.SetSessionStackUpdateCallback(null);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Play 모드에서만 표시됩니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Session Stack", EditorStyles.boldLabel);

            var tick = _target.SessionStackUpdateTick;
            var tickLabel = tick == 0 ? "—" : new DateTime(tick).ToString("HH:mm:ss.fff");
            EditorGUILayout.LabelField("Last Update", tickLabel);

            EditorGUILayout.Space(6);

            var sessions = _target.SessionStack.ToArray();
            if (sessions.Length == 0)
            {
                EditorGUILayout.HelpBox("세션이 없습니다.", MessageType.None);
                return;
            }

            // Stack 시각화: top(index 0) → 위에 표시
            for (int i = 0; i < sessions.Length; i++)
            {
                DrawSessionBox(sessions[i], isCurrent: i == 0);
                if (i < sessions.Length - 1)
                {
                    DrawStackArrow();
                }
            }
        }

        private void DrawSessionBox(PlaySessionBase session, bool isCurrent)
        {
            var sessionColor = GetSessionColor(session.SessionType);
            var prevBg = GUI.backgroundColor;

            // 박스 배경색 적용
            GUI.backgroundColor = sessionColor;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = prevBg;

            // 헤더 행: "SessionType : StageName" + Refresh 버튼(top만)
            var stageId = session.Stage != null ? session.Stage.StageName : "(없음)";
            var headerLabel = $"{session.SessionType} : {stageId}";

            GUI.color = Color.white;
            EditorGUILayout.LabelField(headerLabel, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Status", session.Status.ToString());

            var stage = session.Stage;
            if (stage != null)
            {
                EditorGUILayout.LabelField("StageType", stage.StageType.ToString());
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private static void DrawStackArrow()
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            EditorGUILayout.LabelField("▼", EditorStyles.centeredGreyMiniLabel);
            GUI.color = prevColor;
        }

        private static Color GetSessionColor(PlaySessionType type) => type switch
        {
            PlaySessionType.Exploration => ColorExploration,
            PlaySessionType.Battle      => ColorBattle,
            PlaySessionType.Narrative   => ColorNarrative,
            _                           => ColorUnknown,
        };
    }
}
