using UnityEngine;
using UnityEditor;
using AdBlocker.FMOD.GameStates.Events;

namespace AdBlocker.FMOD.GameStates.Editors {
    public class GSUtilsEditorWindow : EditorWindow {
        private static readonly Vector2 _SIZE = new Vector2(400f, 600f);

        private Vector2 _scroll = Vector2.zero;

        private bool _showGSEAdd = true;
        private GSEventType _selGSEAddType = GSEventType.General;

        private bool _showGSText = true;
        private bool _showGSDBRoute = true;
        private int _GSNumRecentEvents = 10;
        private bool _GSOnlyRecentEvents = true;

        [MenuItem("FMOD/Game State Utils", false, 1000_1)]
        private static void ShowWindow() {
            var window = GetWindow<GSUtilsEditorWindow>(true, "Game State Utils", true);
            window.minSize = _SIZE;
            window.maxSize = _SIZE;
            window.Show();
        }

        private void OnGUI() {
            EditorGUI.indentLevel = 0;

            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Editor must be in play-mode.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _showGSEAdd = EditorGUILayout.Foldout(_showGSEAdd, "Add Game State Event", true);
            if (_showGSEAdd) {
                EditorGUI.indentLevel = 1;
                _selGSEAddType = (GSEventType)EditorGUILayout.EnumPopup("Event Type", _selGSEAddType);
                if (GUILayout.Button("Add Event", GUILayout.Height(50f))) {
                    FMODManagers.GameStateExtractor.AddEvent(InstantiateGameStateEvent(_selGSEAddType));
                    if (!EditorApplication.ExecuteMenuItem("Window/General/Inspector")) Debug.LogWarning("GSUtilsEditorWindow: cannot repaint inspector; expect wrong event serialization");
                }
            }
            EditorGUI.indentLevel = 0;

            _showGSText = EditorGUILayout.Foldout(_showGSText, "Game State Text", true);
            if (_showGSText) {
                EditorGUI.indentLevel = 1;
                _GSNumRecentEvents = Mathf.Max(-1, EditorGUILayout.IntField("Num Recent Events", _GSNumRecentEvents));
                _GSOnlyRecentEvents = EditorGUILayout.Toggle("Only Recent Events", _GSOnlyRecentEvents);
                EditorGUILayout.LabelField(FMODManagers.GameStateExtractor.GetGameStateText(null, _GSNumRecentEvents, _GSOnlyRecentEvents), EditorStyles.textArea);
            }
            EditorGUI.indentLevel = 0;

            _showGSDBRoute = EditorGUILayout.Foldout(_showGSDBRoute, "Game State DB Route", true);
            if (_showGSDBRoute) {
                EditorGUI.indentLevel = 1;
                _GSNumRecentEvents = Mathf.Max(-1, EditorGUILayout.IntField("Num Recent Events", _GSNumRecentEvents));
                EditorGUILayout.LabelField(FMODManagers.GameStateExtractor.GetDBRoute(_GSNumRecentEvents), EditorStyles.textArea);
            }
            EditorGUI.indentLevel = 0;

            EditorGUILayout.EndScrollView();
        }

        private GameStateEvent InstantiateGameStateEvent(GSEventType type) {
            switch (type) {
                case GSEventType.General:
                    return new GEGeneral("");
                case GSEventType.Destruction:
                    return new GEDestruction(new Ads.AdInfos());
                case GSEventType.Chase:
                    return new GEChase(GEChase.CauseType.Unknown, World.DistrictType.Unknown);
                case GSEventType.Escape:
                    return new GEEscape(World.DistrictType.Unknown);
                case GSEventType.Capture:
                    return new GECapture(World.DistrictType.Unknown);
                case GSEventType.Story:
                    return new GEStory("");
                default:
                    goto case GSEventType.General;
            }
        }
    }
}