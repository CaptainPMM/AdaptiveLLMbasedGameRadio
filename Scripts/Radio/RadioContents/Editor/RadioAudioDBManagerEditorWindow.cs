using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AdBlocker.FMOD.Radio.RadioStations;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;
using AdBlocker.FMOD.LLMCommands;
using AdBlocker.FMOD.GameStates.Values;
using AdBlocker.Utils;
using FMOD;

namespace AdBlocker.FMOD.Radio.RadioContents.Editors {
    public class RadioAudioDBManagerEditorWindow : EditorWindow {
        private static readonly Vector2 _SIZE = new Vector2(400f, 600f);

        private Vector2 _scroll;

        private RadioContentCreator.RCCMode _rccTargetMode = RadioContentCreator.RCCMode.Offline;

        private RadioStationType _radioStationType = RadioStationType.None;
        private LLMCommandType _llmCmdType = LLMCommandType.Missing;
        private int _variations = 2;
        private bool _useBackupTTSVoices = false;
        private bool _splitAudioSections = false;

        private List<GeneratedContent> _contents = new();

        private bool _showLLMPrompts = false;
        private bool _showContents = false;

        private int _loadingCounter = 0;
        private int _loadingTarget = 0;
        private float _loadingProgress = 1f;

        [MenuItem("FMOD/RadioAudioDB Manager", false, 1000_3)]
        private static void ShowWindow() {
            var window = GetWindow<RadioAudioDBManagerEditorWindow>(true, "RadioAudioDB Manager", true);
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

            EditorGUI.BeginChangeCheck();
            _rccTargetMode = (RadioContentCreator.RCCMode)EditorGUILayout.EnumPopup("RCC Target Mode", FMODManagers.RadioContentCreator.TargetMode);
            if (EditorGUI.EndChangeCheck()) FMODManagers.RadioContentCreator.TargetMode = _rccTargetMode;

            _radioStationType = (RadioStationType)EditorGUILayout.EnumPopup("Radio Station", _radioStationType);
            EditorGUI.BeginChangeCheck();
            _llmCmdType = (LLMCommandType)EditorGUILayout.EnumPopup("LLM Command Type", _llmCmdType);
            if (EditorGUI.EndChangeCheck()) _splitAudioSections = _llmCmdType == LLMCommandType.Ad; // default for ads
            _variations = EditorGUILayout.IntSlider("Variations", _variations, 1, 10);
            _useBackupTTSVoices = EditorGUILayout.Toggle("Use Backup TTS Voices", _useBackupTTSVoices);
            _splitAudioSections = EditorGUILayout.Toggle("Split Audio Sections", _splitAudioSections);

            GUI.enabled = !IsLoading() && _radioStationType != RadioStationType.None;
            if (GUILayout.Button("Generate LLM Prompts")) GenerateLLMPrompts();
            GUI.enabled = !IsLoading() && _contents.Count > 0;
            if (GUILayout.Button("Generate LLM Content")) GenerateLLMContent();
            if (GUILayout.Button("Generate TTS Content")) GenerateTTSContent();
            if (GUILayout.Button("Write Content to DB")) WriteContentToDB();
            GUI.enabled = true;

            _showLLMPrompts = EditorGUILayout.Foldout(_showLLMPrompts, "LLM Prompts", true);
            if (_showLLMPrompts) {
                EditorGUI.indentLevel = 1;
                if (_contents.Count == 0) {
                    EditorGUILayout.HelpBox("No content generated yet.", MessageType.Info);
                } else {
                    foreach (GeneratedContent content in _contents) {
                        EditorGUILayout.LabelField("DB Route:");
                        EditorGUILayout.LabelField(content.dbRoute, EditorStyles.textArea);
                        if (!string.IsNullOrWhiteSpace(content.llmPrompt.command)) {
                            EditorGUILayout.LabelField("Prompt:");
                            EditorGUILayout.LabelField(content.llmPrompt.ToString(), EditorStyles.textArea);
                            if (GUILayout.Button("Remove")) content.llmPrompt = default;
                        } else {
                            EditorGUILayout.LabelField("[REMOVED]");
                        }
                        EditorGUILayout.Space();
                    }
                }
            }
            EditorGUI.indentLevel = 0;

            _showContents = EditorGUILayout.Foldout(_showContents, "Generated Contents", true);
            if (_showContents) {
                if (_contents.Count == 0) {
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.HelpBox("No content generated yet.", MessageType.Info);
                } else {
                    foreach (GeneratedContent content in _contents) {
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.LabelField($"{content.dbRoute}:");
                        if (content.radioContent != null && content.radioContent.HasException(out Exception ex)) {
                            EditorGUILayout.HelpBox($"Radio Content Error: {ex.Message}", MessageType.Error);
                        } else if (content.radioContent == null || content.radioContent.Sections < 1) {
                            EditorGUILayout.HelpBox("No content generated yet.", MessageType.Info);
                        } else {
                            EditorGUI.indentLevel = 2;
                            for (int i = 0; i < content.radioContent.Sections; i++) {
                                EditorGUILayout.LabelField($"Section {i + 1}:");
                                EditorGUILayout.LabelField("Speaker", (content.radioContent.RadioSpeakers?.Count > i ? content.radioContent.RadioSpeakers[i].SpeakerName : ""), EditorStyles.textArea);
                                EditorGUILayout.LabelField("Text", (content.radioContent.TextContentSections?.Count > i ? content.radioContent.TextContentSections[i] : ""), EditorStyles.textArea);
                                GUI.enabled = content.radioContent.AudioContentSections?.Count > i && content.radioContent.AudioContentSections[i] != null;
                                if (GUILayout.Button("Play Audio")) PlayMP3(content.radioContent.AudioContentSections[i]);
                                GUI.enabled = true;
                                EditorGUILayout.Space();
                            }
                            if (GUILayout.Button("Remove")) content.radioContent = null;
                            EditorGUILayout.Space();
                        }
                    }
                }
            }
            EditorGUI.indentLevel = 0;

            EditorGUILayout.EndScrollView();

            LoadingIndicator();
        }

        private void LoadingIndicator() {
            if (IsLoading()) {
                EditorGUILayout.HelpBox("Loading content - please wait...", MessageType.Info);
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(rect, _loadingProgress, Mathf.RoundToInt(_loadingProgress * 100f).ToString() + "%");
                Repaint();
            }
        }

        private bool IsLoading() {
            return _loadingProgress < 1f;
        }

        private void LoadingStart(int target) {
            _loadingCounter = 0;
            _loadingTarget = target;
            _loadingProgress = 0f;
        }

        private void LoadingProgress() {
            _loadingProgress = (float)++_loadingCounter / (float)_loadingTarget;
        }

        private void LoadingFinished() {
            _loadingProgress = 1f;
        }

        private void GenerateLLMPrompts() {
            RadioStation radioStation = FMODManagers.RadioManager.RadioStationsDict[_radioStationType];

            List<WeightedValue<LLMCommand>> commands = radioStation.GetLLMCommands(_llmCmdType);
            if (commands.Count == 0) {
                UnityEngine.Debug.LogWarning($"RadioAudioDBManager: GenerateLLMPrompts: radio station does not have fitting commands for the type <{_llmCmdType}>");
                return;
            }

            LoadingStart(commands.Count);
            _contents.Clear();
            foreach (WeightedValue<LLMCommand> cmd in commands) {
                GetGameStates(cmd.value, out var gameStates);

                for (int i = 0; i < gameStates.Count; i++) {
                    for (int v = 0; v < _variations; v++) {
                        // (TODO the speaker combinations could be iterated as well)
                        var speakerRoles = cmd.value.Roles;
                        List<RadioSpeaker> speakers = new();
                        foreach (var speakerRole in speakerRoles) speakers.Add(radioStation.GetRadioSpeaker(speakerRole));

                        // Set up the game state for the current prompt
                        if (gameStates[i] != null)
                            FMODManagers.GameStateExtractor.OverrideGameStateValue(gameStates[i].Type, gameStates[i]);

                        // (TODO the prompt objective parameter combinations could be iterated as well)
                        GeneratedContent content = new GeneratedContent();
                        content.dbRoute = RadioAudioDB.GetDBRoute(radioStation, cmd.value.CmdType, cmd.weight, gameStates[i] != null ? gameStates[i].DBRoute : "");
                        content.llmPrompt = cmd.value.GetPrompt(radioStation, speakers, cmd.value.Roles);
                        content.speakers = speakers;
                        _contents.Add(content);
                    }
                }

                LoadingProgress();
            }

            LoadingFinished();
        }

        private void GetGameStates(LLMCommand cmd, out List<GameStateValue> gameStates) {
            // (TODO currently only PlayerReputation is supported)
            // (... if multiple game state values should be possible this implementation must be changed to cover all combinations)
            gameStates = new();
            foreach (GSValueType gsvType in cmd.CtxGameStateValueTypes) {
                switch (gsvType) {
                    case GSValueType.PlayerReputation:
                        gameStates.AddRange(new GVPlayerReputation().GetOfflineStates());
                        break;
                }
            }
            if (gameStates.Count == 0) gameStates.Add(null);
        }

        private async void GenerateLLMContent() {
            LoadingStart(_contents.Count);

            _showContents = true;
            foreach (GeneratedContent content in _contents) {
                if (!string.IsNullOrWhiteSpace(content.llmPrompt.command)) {
                    content.radioContent = await FMODManagers.RadioContentCreator.CreateContentLLM(content.llmPrompt, content.speakers);
                }
                LoadingProgress();
            }

            LoadingFinished();
        }

        private async void GenerateTTSContent() {
            LoadingStart(_contents.Count);

            _showContents = true;
            foreach (GeneratedContent content in _contents) {
                if (content.radioContent != null && content.radioContent.TextContentSections?.Count > 0 && !content.radioContent.HasException(out _)) {
                    content.radioContent = await FMODManagers.RadioContentCreator.CreateContentTTS(content.radioContent, _useBackupTTSVoices);
                }
                LoadingProgress();
            }

            LoadingFinished();
        }

        private void WriteContentToDB() {
            LoadingStart(_contents.Count);

            foreach (GeneratedContent content in _contents) {
                if (content.radioContent != null && content.radioContent.ConcatAudioContentSections?.Length > 0 && !content.radioContent.HasException(out _)) {
                    RadioAudioDB.Write(content.dbRoute, content.radioContent, _splitAudioSections);
                }
                LoadingProgress();
            }

            LoadingFinished();
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

        private class GeneratedContent {
            public string dbRoute;
            public LLMPrompt llmPrompt;
            public List<RadioSpeaker> speakers;
            public RadioContent radioContent;

            public GeneratedContent() {
                dbRoute = "";
                llmPrompt = default;
                speakers = new();
                radioContent = null;
            }
        }
    }
}