using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using AdBlocker.FMOD.Radio.RadioStations;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;
using AdBlocker.FMOD.LLMCommands;
using FMOD;

namespace AdBlocker.FMOD.Radio.RadioContents.Editors {
    public class APIDebuggerEditorWindow : EditorWindow {
        private static readonly Vector2 _SIZE = new Vector2(400f, 600f);

        private Vector2 _scroll;

        private RadioContentType _radioContentType;
        private LLMCommandType _llmCommandType;
        private bool _loadingContent;

        private bool _showRadioSettings = true;
        private RadioStationType _activeRadioStatioType;
        private RadioContentCreator.RCCMode _rccTargetMode;

        private bool _showOfflineHandling;
        private LLMCommand _llmCmd;
        private string _dbRoute;

        private bool _showLLMHandling;
        private bool _showLLMPrompt;
        private bool _llmPromptCommandOnly;
        private LLMPrompt _llmPrompt;
        private List<RadioSpeaker> _speakers;
        private bool _showLLMUnvalidatedResult;
        private string _llmUnvalidatedResult;

        private bool _showRadioContent = true;
        private RadioContent _radioContent;

        [MenuItem("FMOD/API Debugger", false, 1000_2)]
        private static void ShowWindow() {
            var window = GetWindow<APIDebuggerEditorWindow>(true, "API Debugger", true);
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

            _radioContentType = (RadioContentType)EditorGUILayout.EnumPopup("Radio Content Type", _radioContentType);
            _llmCommandType = (LLMCommandType)EditorGUILayout.EnumPopup("LLM Command Type", _llmCommandType);

            GUI.enabled = !_loadingContent;
            if (GUILayout.Button("Create Content", GUILayout.Height(50f))) CreateContent();
            if (GUILayout.Button("Create Content (Debug)", GUILayout.Height(25f))) CreateContentDebug();
            GUI.enabled = true;

            _showRadioSettings = EditorGUILayout.Foldout(_showRadioSettings, "Radio Settings", true);
            if (_showRadioSettings) {
                EditorGUI.indentLevel = 1;
                EditorGUI.BeginChangeCheck();
                _activeRadioStatioType = (RadioStationType)EditorGUILayout.EnumPopup("Radio Station", FMODManagers.RadioManager.ActiveRadioStation);
                if (EditorGUI.EndChangeCheck()) FMODManagers.RadioManager.SetActiveRadioStation(_activeRadioStatioType);

                EditorGUI.BeginChangeCheck();
                _rccTargetMode = (RadioContentCreator.RCCMode)EditorGUILayout.EnumPopup("RCC Target Mode", FMODManagers.RadioContentCreator.TargetMode);
                if (EditorGUI.EndChangeCheck()) FMODManagers.RadioContentCreator.TargetMode = _rccTargetMode;
                EditorGUILayout.LabelField($"RCC Mode", FMODManagers.RadioContentCreator.Mode.ToString());
            }
            EditorGUI.indentLevel = 0;

            _showOfflineHandling = EditorGUILayout.Foldout(_showOfflineHandling, "Offline Handling", true);
            if (_showOfflineHandling) {
                EditorGUI.indentLevel = 1;
                GUI.enabled = !_loadingContent;
                if (GUILayout.Button("1. Create Content (Offline)", GUILayout.Height(25f))) CreateContentOffline();
                GUI.enabled = true;

                EditorGUILayout.LabelField("DB Route:");
                EditorGUILayout.LabelField(_dbRoute, EditorStyles.textArea);
            }
            EditorGUI.indentLevel = 0;

            _showLLMHandling = EditorGUILayout.Foldout(_showLLMHandling, "LLM Handling", true);
            if (_showLLMHandling) {
                EditorGUI.indentLevel = 1;

                GUI.enabled = !_loadingContent;
                if (GUILayout.Button("1. Create Content (Command)", GUILayout.Height(25f))) CreateContentLLMCommand();
                EditorGUILayout.LabelField("Command:", _llmCmd != null ? _llmCmd.CmdType.ToString() : "Missing");
                GUI.enabled = !_loadingContent && _llmCmd != null;
                if (GUILayout.Button("2. Create Content (Prompt)", GUILayout.Height(25f))) CreateContentLLMPrompt();
                GUI.enabled = true;

                _showLLMPrompt = EditorGUILayout.Foldout(_showLLMPrompt, "LLM Prompt", true);
                if (_showLLMPrompt) {
                    EditorGUI.indentLevel = 2;
                    if (GUILayout.Button("Copy LLM Prompt Content")) GUIUtility.systemCopyBuffer = _llmPrompt.fullTxt;
                    _llmPromptCommandOnly = EditorGUILayout.Toggle("Show command only", _llmPromptCommandOnly);
                    EditorGUILayout.LabelField(_llmPromptCommandOnly ? _llmPrompt.command : _llmPrompt.fullTxt, EditorStyles.textArea);
                }
                EditorGUI.indentLevel = 1;

                GUI.enabled = !_loadingContent && !string.IsNullOrWhiteSpace(_llmPrompt.fullTxt) && _speakers != null;
                if (GUILayout.Button("3. Create Content (LLM)", GUILayout.Height(25f))) _ = CreateContentLLM();
                GUI.enabled = true;

                _showLLMUnvalidatedResult = EditorGUILayout.Foldout(_showLLMUnvalidatedResult, "LLM Unvalidated Result", true);
                if (_showLLMUnvalidatedResult) {
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.LabelField(_llmUnvalidatedResult, EditorStyles.textArea);
                }
            }
            EditorGUI.indentLevel = 0;

            _showRadioContent = EditorGUILayout.Foldout(_showRadioContent, "Radio Content", true);
            if (_showRadioContent) {
                EditorGUI.indentLevel = 1;

                GUI.enabled = !_loadingContent && _radioContent?.TextContentSections?.Count > 0 && !_radioContent.HasException(out _);
                if (GUILayout.Button("4. Create Content (TTS)", GUILayout.Height(25f))) _ = CreateContentTTS();
                GUI.enabled = true;

                if (_radioContent != null && _radioContent.HasException(out Exception ex)) {
                    EditorGUILayout.HelpBox($"Radio Content Error: {ex.Message}", MessageType.Error);
                } else if (_radioContent == null || _radioContent.Sections < 1) {
                    EditorGUILayout.HelpBox("No Radio Content generated yet.", MessageType.Info);
                } else {
                    for (int i = 0; i < _radioContent.Sections; i++) {
                        EditorGUILayout.LabelField($"Section {i + 1}:");
                        EditorGUILayout.LabelField("Speaker", (_radioContent.RadioSpeakers?.Count > i ? _radioContent.RadioSpeakers[i].SpeakerName : ""), EditorStyles.textArea);
                        EditorGUILayout.LabelField("Text", (_radioContent.TextContentSections?.Count > i ? _radioContent.TextContentSections[i] : ""), EditorStyles.textArea);
                        GUI.enabled = _radioContent.AudioContentSections?.Count > i && _radioContent.AudioContentSections[i] != null;
                        if (GUILayout.Button("Play Audio")) PlayMP3(_radioContent.AudioContentSections[i]);
                        GUI.enabled = true;
                        EditorGUILayout.Space();
                    }
                }
            }
            EditorGUI.indentLevel = 0;

            EditorGUILayout.EndScrollView();

            LoadingIndicator();
        }

        private void LoadingIndicator() {
            if (_loadingContent) {
                EditorGUILayout.HelpBox("Loading content - please wait...", MessageType.Info);
                Repaint();
            }
        }

        private async void CreateContent() {
            _loadingContent = true;
            _radioContent = await FMODManagers.RadioContentCreator.CreateContent(FMODManagers.RadioManager.GetActiveRadioStationData(), _radioContentType, _llmCommandType);
            _loadingContent = false;

            _showRadioContent = true;
        }

        private async void CreateContentDebug() {
            CreateContentOffline();
            if (FMODManagers.RadioContentCreator.Mode == RadioContentCreator.RCCMode.Offline) return;
            CreateContentLLMPrompt();
            await CreateContentLLM();
            if (FMODManagers.RadioContentCreator.Mode != RadioContentCreator.RCCMode.Online) return;
            await CreateContentTTS();
        }

        private bool CreateContentLLMCommand() {
            RadioStation radioStation = FMODManagers.RadioManager.GetActiveRadioStationData();
            if (!radioStation) {
                UnityEngine.Debug.LogWarning($"API Debugger: radio station is null");
                return false;
            }
            if (_radioContentType == RadioContentType.Missing && _llmCommandType == LLMCommandType.Missing) {
                UnityEngine.Debug.LogWarning($"API Debugger: received only Missing content and command type");
                return false;
            }

            _loadingContent = true;
            bool res = FMODManagers.RadioContentCreator.GetLLMCommand(radioStation, _radioContentType, _llmCommandType, out _llmCmd);
            _loadingContent = false;

            return res;
        }

        private async void CreateContentOffline() {
            _loadingContent = true;
            if (CreateContentLLMCommand()) {
                _radioContent = await FMODManagers.RadioContentCreator.CreateContentOffline(FMODManagers.RadioManager.GetActiveRadioStationData(), _llmCmd, (dbRoute) => _dbRoute = dbRoute);

                _showOfflineHandling = true;
                _showRadioContent = true;
            }
            _loadingContent = false;
        }

        private void CreateContentLLMPrompt() {
            _loadingContent = true;
            if (_llmCmd != null) {
                FMODManagers.RadioContentCreator.GetLLMPrompt(FMODManagers.RadioManager.GetActiveRadioStationData(), _llmCmd, out _llmPrompt, out _speakers);

                _showLLMHandling = true;
                _showLLMPrompt = true;
            }
            _loadingContent = false;
        }

        private async Task CreateContentLLM() {
            _loadingContent = true;
            if (!string.IsNullOrWhiteSpace(_llmPrompt.fullTxt) && _speakers != null) {
                _radioContent = await FMODManagers.RadioContentCreator.CreateContentLLM(_llmPrompt, _speakers, (llmUnvalidatedResult) => _llmUnvalidatedResult = llmUnvalidatedResult);

                _showLLMHandling = true;
                _showLLMUnvalidatedResult = true;
                _showRadioContent = true;
            }
            _loadingContent = false;
        }

        private async Task CreateContentTTS() {
            _loadingContent = true;
            if (_radioContent != null && !_radioContent.HasException(out _)) {
                _radioContent = await FMODManagers.RadioContentCreator.CreateContentTTS(_radioContent);

                _showRadioContent = true;
            }
            _loadingContent = false;
        }

        private void PlayMP3(byte[] mp3Data) {
            var soundInfo = new CREATESOUNDEXINFO() {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = (uint)mp3Data.Length,
                suggestedsoundtype = SOUND_TYPE.MPEG
            };

            FMODUnity.RuntimeManager.CoreSystem.createSound(mp3Data, MODE.OPENMEMORY_POINT | MODE.CREATECOMPRESSEDSAMPLE, ref soundInfo, out var sound);
            FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var channelGroup);
            FMODUnity.RuntimeManager.CoreSystem.playSound(sound, channelGroup, false, out _);
        }
    }
}