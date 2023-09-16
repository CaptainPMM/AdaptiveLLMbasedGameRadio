using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using AdBlocker.FMOD.Web;
using AdBlocker.FMOD.Radio.RadioContents.APIModels.LLM;
using AdBlocker.FMOD.Radio.RadioContents.APIModels.TTS;
using AdBlocker.FMOD.Radio.RadioStations;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;
using AdBlocker.FMOD.LLMCommands;
using AdBlocker.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdBlocker.FMOD.Radio.RadioContents {
    public class RadioContentCreator : MonoBehaviour {
        public const string PREFS_KEY_RCC_TARGETMODE = "adblocker.radio.rcc.targetmode";
        private const string _PREFS_KEY_API_TOKEN_LLM = "adblocker.radio.api.token.llm";
        private const string _PREFS_KEY_API_TOKEN_TTS = "adblocker.radio.api.token.tts";
        private const string _PREFS_KEY_API_MODEL_LLM = "adblocker.radio.api.model.llm";
        private const string _PREFS_KEY_API_MODEL_TTS = "adblocker.radio.api.model.tts";

        private static readonly KeyValuePair<string, string> _API_HEADER_CONTENT_JSON = new("Content-Type", "application/json");
        private static readonly KeyValuePair<string, string> _API_HEADER_ACCEPT_JSON = new("Accept", "application/json");
        private static readonly KeyValuePair<string, string> _API_HEADER_ACCEPT_AUDIO = new("Accept", "audio/mpeg");

        public const string LLM_TEXT_VALIDATION_REGEX_STRING = @"^([^:\n]+):\s*""(.+)""";
        private static readonly Regex _LLM_TEXT_VALIDATION_REGEX = new Regex(LLM_TEXT_VALIDATION_REGEX_STRING, RegexOptions.Multiline);

        [Header("Settings/General")]
        [SerializeField] private RCCMode _targetMode = RCCMode.Offline;
        [SerializeField, Min(0)] private int _modeUpdateInterval = 30_000; // in ms
        [SerializeField, Min(0)] private int _webRequestTimeout = 30; // in s

        [Header("Settings/LLM")]
        [SerializeField] private string _apiUrlLLM = "https://www.google.com/";
        [SerializeField] private string _apiDefaultModelLLM = "gpt-3.5-turbo-0301";
        [SerializeField, Range(0f, 2f)] private float _apiLLMTemperature = LLMRequest.DEFAULT_TEMPERATURE;
        [SerializeField] private uint _apiLLMMaxTokens = LLMRequest.DEFAULT_MAX_TOKENS;
        [field: SerializeField, Multiline(5)] public string LLMGeneralCtx { get; private set; } = "";

        [Header("Settings/TTS")]
        [SerializeField] private string _apiUrlTTS = "https://www.google.com/";
        [SerializeField] private string _apiDefaultModelTTS = "eleven_monolingual_v1";

        [field: Header("Runtime/General")]
        [field: SerializeField, ReadOnly] public RCCMode Mode { get; private set; }

        [field: Header("Runtime/TTS")]
        [SerializeField] private List<TTSVoice> _TTSVoices = new();

        [Header("Debug/Setup")]
        [SerializeField, Setup] private Canvas _debugUI;

        [Header("Debug/Settings")]
        [SerializeField] private bool _modeUpdateLogs = true;
        [SerializeField] private bool _modeUpdateRoutineLogs = false;

        public RCCMode TargetMode {
            get => _targetMode;
            set {
                _targetMode = value;
                if (Mode != _targetMode) _ = ModeUpdate(_modeUpdateLogs);

#if UNITY_EDITOR
                _oldTargetMode = _targetMode;
#endif
            }
        }

        private string _apiTokenLLM;
        private string _apiTokenTTS;
        private KeyValuePair<string, string> _apiHeaderLLMAuth;
        private KeyValuePair<string, string> _apiHeaderTTSAuth;

        private string _apiModelLLM;
        private string _apiModelTTS;

        private Dictionary<LLMCommandType, RadioContent> _commandsCtx = new();

        private void Awake() {
            if (!_debugUI) Debug.LogWarning("RCC: no debug UI assigned");

#if UNITY_EDITOR
            _oldTargetMode = _targetMode;
#endif
        }

        private void Start() {
            _targetMode = (RCCMode)PlayerPrefs.GetInt(PREFS_KEY_RCC_TARGETMODE, (int)_targetMode);

            // Set default API model settings if not set yet
            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(_PREFS_KEY_API_MODEL_LLM, ""))) {
                PlayerPrefs.SetString(_PREFS_KEY_API_MODEL_LLM, _apiDefaultModelLLM);
                PlayerPrefs.Save();
            }
            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(_PREFS_KEY_API_MODEL_TTS, ""))) {
                PlayerPrefs.SetString(_PREFS_KEY_API_MODEL_TTS, _apiDefaultModelTTS);
                PlayerPrefs.Save();
            }

            _ = ModeUpdate(_modeUpdateLogs);
            ModeUpdateRoutine();

            _debugUI.gameObject.SetActive(false);
        }

#if ENABLE_DEBUG_FEATURES
        private void Update() {
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.digit8Key.wasPressedThisFrame) TargetMode = RCCMode.TextOnly;
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.digit9Key.wasPressedThisFrame) TargetMode = RCCMode.Online;
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.digit0Key.wasPressedThisFrame) TargetMode = RCCMode.Offline;
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.uKey.wasPressedThisFrame) ToggleDebugUI();

#if UNITY_EDITOR
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.iKey.wasPressedThisFrame) {
                if (!EditorApplication.ExecuteMenuItem("FMOD/API Debugger")) Debug.LogWarning("RCC Editor: shortcut link missing");
            }
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.oKey.wasPressedThisFrame) {
                if (!EditorApplication.ExecuteMenuItem("FMOD/Game State Utils")) Debug.LogWarning("RCC Editor: shortcut link missing");
            }
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.pKey.wasPressedThisFrame) {
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEDestruction(new Ads.AdInfos(Ads.AdType.Billboard, Ads.AdImportance.Low, "Showing the NeroLink chip on the street.", World.DistrictType.NewHaven)));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEChase(GameStates.Events.GEChase.CauseType.Destruction, World.DistrictType.NewHaven));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEEscape(World.DistrictType.NewHaven));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEDestruction(new Ads.AdInfos(Ads.AdType.Billboard, Ads.AdImportance.Low, "Showing the NeroLink chip in a back alley.", World.DistrictType.NewHaven)));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEChase(GameStates.Events.GEChase.CauseType.Destruction, World.DistrictType.NewHaven));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEEscape(World.DistrictType.NewHaven));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEDestruction(new Ads.AdInfos(Ads.AdType.Billboard, Ads.AdImportance.Medium, "Showing the NeroLink chip on a plaza.", World.DistrictType.NewHaven)));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GEChase(GameStates.Events.GEChase.CauseType.Destruction, World.DistrictType.NewHaven));
                FMODManagers.GameStateExtractor.AddEvent(new AdBlocker.FMOD.GameStates.Events.GECapture(World.DistrictType.NewHaven));
            }
#endif
        }
#endif

        public void SetAPITokenLLM(string token) {
            _apiTokenLLM = token;
            PlayerPrefs.SetString(_PREFS_KEY_API_TOKEN_LLM, token);
            PlayerPrefs.Save();
            _ = ModeUpdate(_modeUpdateLogs);
        }

        public void SetAPITokenTTS(string token) {
            _apiTokenTTS = token;
            PlayerPrefs.SetString(_PREFS_KEY_API_TOKEN_TTS, token);
            PlayerPrefs.Save();
            _ = ModeUpdate(_modeUpdateLogs);
        }

        public void SetAPIModelLLM(string model) {
            _apiModelLLM = model;
            PlayerPrefs.SetString(_PREFS_KEY_API_MODEL_LLM, model);
            PlayerPrefs.Save();
            _ = ModeUpdate(_modeUpdateLogs);
        }

        public void SetAPIModelTTS(string model) {
            _apiModelTTS = model;
            PlayerPrefs.SetString(_PREFS_KEY_API_MODEL_TTS, model);
            PlayerPrefs.Save();
            _ = ModeUpdate(_modeUpdateLogs);
        }

        /// <summary>
        /// Generate radio content based on the content type.
        /// Adds used command to command context.
        /// </summary>
        /// <param name="radioStation">radio station (determines radio content parameters)</param>
        /// <param name="rcType">content type</param>
        /// <returns>radio content (check HasException())</returns>
        public async Task<RadioContent> CreateContent(RadioStation radioStation, RadioContentType rcType) {
            return await CreateContent(radioStation, rcType, LLMCommandType.Missing);
        }

        /// <summary>
        /// Generate radio content based on the command type.
        /// Adds used command to command context.
        /// </summary>
        /// <param name="radioStation">radio station (determines radio content parameters)</param>
        /// <param name="cmdType">command type</param>
        /// <returns>radio content (check HasException())</returns>
        public async Task<RadioContent> CreateContent(RadioStation radioStation, LLMCommandType cmdType) {
            return await CreateContent(radioStation, RadioContentType.Missing, cmdType);
        }

        /// <summary>
        /// Generate radio content based on a content type OR command type.
        /// Adds used command to command context.
        /// Only one type must be set to another value than Missing.
        /// If both values are set, the command type is used and radio content type ignored.
        /// </summary>
        /// <param name="radioStation">radio station (determines radio content parameters)</param>
        /// <param name="rcType">content type (ignored if cmdType is set to other than Missing)</param>
        /// <param name="cmdType">command type</param>
        /// <returns>radio content (check HasException())</returns>
        public async Task<RadioContent> CreateContent(RadioStation radioStation, RadioContentType rcType, LLMCommandType cmdType) {
            if (!radioStation) {
                Debug.LogWarning($"RCC: CreateContent: radio station is null");
                return new RadioContent(new RadioContent.RCException("radio station is null"));
            }
            if (rcType == RadioContentType.Missing && cmdType == LLMCommandType.Missing) {
                Debug.LogWarning($"RCC: CreateContent: received only Missing content and command type");
                return new RadioContent(new RadioContent.RCException("received only Missing content and command type"));
            }

            // OFFLINE HANDLING
            if (!GetLLMCommand(radioStation, rcType, cmdType, out LLMCommand llmCmd)) return new RadioContent(new RadioContent.RCException("GetLLMCommand error"));

            if (Mode == RCCMode.Offline) return await CreateContentOffline(radioStation, llmCmd);

            // TEXT ONLY HANDLING
            if (!GetLLMPrompt(radioStation, llmCmd, out LLMPrompt llmPrompt, out List<RadioSpeaker> speakers)) return new RadioContent(new RadioContent.RCException("GetLLMPrompt error")); ;

            RadioContent radioContent = await CreateContentLLM(llmPrompt, speakers);
            if (radioContent.HasException(out Exception llmEx)) {
                // LLM API error occured -> retry in offline mode...
                Mode = RCCMode.Offline;
                Debug.LogWarning($"RCC: CreateContentTextOnly: {llmEx.Message}");
                Debug.Log($"RCC: CreateContent: retrying in {Mode.ToString()} mode...");
                Debug.Log($"RCC: [{Mode.ToString()}]");
                return await CreateContentOffline(radioStation, llmCmd);
            }

            if (Mode != RCCMode.Online) {
                _commandsCtx[llmCmd.CmdType] = radioContent;
                return radioContent;
            }

            // ONLINE HANDLING
            radioContent = await CreateContentTTS(radioContent);

            _commandsCtx[llmCmd.CmdType] = radioContent;
            return radioContent;
        }

        /// <summary>
        /// Only one type must be set to another value than Missing.
        /// If both values are set, the command type is used and radio content type ignored.
        /// </summary>
        /// <returns>false, on error and outputs LLM command</returns>
        public bool GetLLMCommand(RadioStation radioStation, RadioContentType rcType, LLMCommandType cmdType, out LLMCommand llmCmd) {
            llmCmd = null;

            // Get fitting command based on radio content type or command type
            // TODO comments special handling (always offline)
            if (cmdType != LLMCommandType.Missing) llmCmd = radioStation.GetLLMCommand(cmdType);
            else llmCmd = radioStation.GetLLMCommand(rcType);

            if (llmCmd == null) {
                Debug.LogWarning($"RCC: GetLLMCommand: no fitting command at station <{radioStation.Name}> for content type <{rcType}> or command type <{cmdType}>");
                return false;
            }
            return true;
        }

        /// <returns>false, on error and outputs LLM prompt and used speakers</returns>
        public bool GetLLMPrompt(RadioStation radioStation, LLMCommand llmCmd, out LLMPrompt llmPrompt, out List<RadioSpeaker> speakers) {
            llmPrompt = default;
            speakers = new();

            var speakerRoles = llmCmd.Roles;
            if (speakerRoles.Count == 0) {
                Debug.LogWarning($"RCC: GetLLMPrompt: no speaker roles defined at station <{radioStation.Name}> for command type <{llmCmd.CmdType}>");
                return false;
            }

            // Get fitting radio speakers
            foreach (var speakerRole in speakerRoles) speakers.Add(radioStation.GetRadioSpeaker(speakerRole));

            // Get command context
            string llmCmdCtx = "";
            if (_commandsCtx.TryGetValue(llmCmd.CmdType, out RadioContent cmdCtxObj)) llmCmdCtx = string.Join('\n', cmdCtxObj.TextContentSections);

            // Generate LLM prompt
            llmPrompt = llmCmd.GetPrompt(radioStation, speakers, speakerRoles, llmCmdCtx);
            return true;
        }

        /// <returns>radio content (check HasException())</returns>
        public async Task<RadioContent> CreateContentOffline(RadioStation radioStation, LLMCommand llmCommand, Action<string> onDBRouteResult = null) {
            return await RadioAudioDB.Query(radioStation, llmCommand, onDBRouteResult);
        }

        /// <param name="llmPrompt">LLMPrompt to send to the API</param>
        /// <param name="speakers">radio speakers used</param>
        /// <param name="onLLMUnvalidatedResult">may be used to debug the raw LLM response (callback providing the string)</param>
        /// <returns>radio content (check HasException())</returns>
        public async Task<RadioContent> CreateContentLLM(LLMPrompt llmPrompt, List<RadioSpeaker> speakers, Action<string> onLLMUnvalidatedResult = null) {
            if (Mode == RCCMode.Offline) return new RadioContent(new RadioContent.RCException("RCC is in [Offline] mode"));

            LLMRequest llmRequestBody = new LLMRequest(_apiModelLLM, new LLMRequest.Message[] {
                            new(LLMRequest.Message.Role.system, llmPrompt.system),
                            new(LLMRequest.Message.Role.user, llmPrompt.command + (string.IsNullOrWhiteSpace(llmPrompt.outputParams) ? "" : $"\n\n{llmPrompt.outputParams}"))
                        }, _apiLLMTemperature, _apiLLMMaxTokens);

            WebResult llmWebResult;
            try {
                llmWebResult = await WebRequest.Post($"{_apiUrlLLM}chat/completions", llmRequestBody.ToJSON(), BuildAPIHeaders(_API_HEADER_CONTENT_JSON, _apiHeaderLLMAuth), _webRequestTimeout);
                if (!this) throw new RadioContent.RCException("application quit while awaiting the LLM result");
                if (llmWebResult.error) throw new RadioContent.RCException(llmWebResult.errorMsg);
            } catch (Exception e) {
                return new RadioContent(e);
            }

            // LLM response parsing and validation (speaker names and output params currently not handled)
            string llmUnvalidatedResult = LLMResponse.FromJSON(llmWebResult.result).GetContent();
            onLLMUnvalidatedResult?.Invoke(llmUnvalidatedResult);
            if (!ValidateLLMResult(llmUnvalidatedResult, speakers.Count, out _, out List<string> speechTexts, out _)) {
                return new RadioContent(new RadioContent.RCException("LLM API result content is invalid"));
            }

            return new RadioContent(llmPrompt.contentType, llmPrompt, (byte)speechTexts.Count, speakers, speechTexts);
        }

        /// <returns>radio content (check HasException())</returns>
        public async Task<RadioContent> CreateContentTTS(RadioContent textOnlyContent, bool useBackupVoices = false) {
            if (Mode != RCCMode.Online) return new RadioContent(new RadioContent.RCException("RCC is not in [Online] mode"));

            // Generate audio content for each speaker section
            Task<WebResult>[] ttsWebRequests = new Task<WebResult>[textOnlyContent.RadioSpeakers.Count];
            for (int i = 0; i < textOnlyContent.RadioSpeakers.Count; i++) {
                RadioSpeaker speaker = textOnlyContent.RadioSpeakers[i];
                string voiceID = _TTSVoices.Find(v => v.name == speaker.VoiceName).voice_id;
                bool backupUsed = useBackupVoices;
                if (useBackupVoices || string.IsNullOrWhiteSpace(voiceID)) {
                    // Preferred voice not found -> use backup voice
                    voiceID = _TTSVoices.Find(v => v.name == speaker.BackupPremadeVoiceName).voice_id;
                    backupUsed = true;

                    if (string.IsNullOrWhiteSpace(voiceID)) {
                        // No voice found -> process next voice
                        Debug.LogWarning($"RCC: CreateContentOnline: could not find fitting voice for <{speaker.name}> (backup <{speaker.BackupPremadeVoiceName}> also not found)");
                        ttsWebRequests[i] = Task.FromResult<WebResult>(new WebResult(null, null, null, true, "no fitting voice found")); // add finished task to not halt Task.WhenAll()
                        continue;
                    }
                }
                TTSRequest ttsReqBody = new TTSRequest() {
                    voice_settings = !backupUsed ? speaker.VoiceSettings : speaker.BackupVoiceSettings,
                    model_id = _apiModelTTS,
                    text = textOnlyContent.TextContentSections[i]
                };
                ttsWebRequests[i] = WebRequest.Post($"{_apiUrlTTS}text-to-speech/{voiceID}", ttsReqBody.ToJSON(), BuildAPIHeaders(_API_HEADER_ACCEPT_AUDIO, _API_HEADER_CONTENT_JSON, _apiHeaderTTSAuth), _webRequestTimeout);
            }

            // Wait for responses...
            WebResult[] ttsWebResults = await Task.WhenAll(ttsWebRequests);
            if (!this) return new RadioContent(new RadioContent.RCException("application quit while awaiting the TTS result"));

            List<byte[]> audioContentSections = new();
            bool atLeastOneAudio = false;
            for (int i = 0; i < ttsWebResults.Length; i++) {
                WebResult ttsWebResult = ttsWebResults[i];
                if (ttsWebResult.error) {
                    audioContentSections.Add(null);
                    Debug.LogWarning($"RCC: CreateContentOnline section {i}: TTS API response error: {ttsWebResult.errorMsg}");
                    continue;
                }

                if (ttsWebResult.data.Length == 0) {
                    audioContentSections.Add(null);
                    Debug.LogWarning($"RCC: CreateContentOnline section {i}: empty data");
                    continue;
                }

                audioContentSections.Add(ttsWebResult.data);
                atLeastOneAudio = true;
                // (TODO one could remove the 11labs history item, but not necessary)
            }
            if (!atLeastOneAudio) {
                // No audio files -> fallback to TextOnly result and set Mode temporarily
                Mode = RCCMode.TextOnly;
                Debug.LogWarning($"RCC: CreateContentOnline: no audio files fetched");
                Debug.Log($"RCC: CreateContentOnline: falling back to TextOnly result");
                Debug.Log($"RCC: [{Mode.ToString()}]");
                return textOnlyContent;
            }

            return textOnlyContent.TextOnlyToOnlineUpgrade(audioContentSections);
        }

        /// <summary>
        /// Validates and processes the raw LLM response content.
        /// Returns false and null collections if the content is invalid.
        /// Outputs the speaker names and associated speech texts as lists in the same order.
        /// Also outputs the output params as dict.
        /// </summary>
        /// <param name="unvalidatedResult">raw LLM response content</param>
        /// <param name="speakersCount">number of intended radio speakers, checked against number of speaker sections and overflow sections are removed</param>
        /// <param name="speakerNames">speaker names</param>
        /// <param name="speechTexts">speech texts</param>
        /// <param name="outputParams">output parameters dict</param>
        /// <returns>True, if LLM response content is valid. False and null collections, otherwise</returns>
        private bool ValidateLLMResult(string unvalidatedResult, int speakersCount, out List<string> speakerNames, out List<string> speechTexts, out Dictionary<string, string> outputParams) {
            speakerNames = null;
            speechTexts = null;
            outputParams = null;

            // General null or empty validation
            if (string.IsNullOrWhiteSpace(unvalidatedResult)) {
                Debug.LogWarning("RCC: ValidateLLMResult: content is null or empty");
                return false;
            }

            // Sections validation
            MatchCollection matches = _LLM_TEXT_VALIDATION_REGEX.Matches(unvalidatedResult);
            if (matches.Count == 0) {
                Debug.LogWarning($"RCC: ValidateLLMResult: content is irregular: {unvalidatedResult}");
                return false;
            }

            speakerNames = new();
            speechTexts = new();
            foreach (Match match in matches) {
                if (speechTexts.Count >= speakersCount) break;
                speakerNames.Add(match.Groups[1].Value);
                speechTexts.Add(match.Groups[2].Value);
            }

            // Get output params
            string[] splittedOutputParams = unvalidatedResult.Split(LLMCommand.OUTPUT_PARAMS_SEPARATOR);
            if (splittedOutputParams.Length == 2) {
                outputParams = new();
                string[] parameters = splittedOutputParams[1].Split(',');
                foreach (string param in parameters) {
                    string[] kv = param.Split(':');
                    outputParams.Add(kv[0].Trim(' '), kv[1].Trim(' '));
                }
            }

            return true;
        }

        private async void ModeUpdateRoutine() {
            await Task.Delay(_modeUpdateInterval);
            while (this) { // as long as the object exists -> prevents continuation after play stop
                await ModeUpdate(_modeUpdateRoutineLogs);
                await Task.Delay(_modeUpdateInterval);
            }
        }

        private async Task ModeUpdate(bool logging = false) {
            switch (_targetMode) {
                case RCCMode.Offline:
                    Mode = RCCMode.Offline;
                    if (logging) Debug.Log("RCC: [Offline]");
                    break;
                case RCCMode.TextOnly:
                case RCCMode.Online:
                    // Check basic internet reachability
                    if (Application.internetReachability == NetworkReachability.NotReachable) {
                        if (logging) Debug.LogWarning("RCC: internet not reachable");
                        goto case RCCMode.Offline;
                    }

                    // Check API tokens & models
                    _apiTokenLLM = PlayerPrefs.GetString(_PREFS_KEY_API_TOKEN_LLM, "");
                    if (string.IsNullOrWhiteSpace(_apiTokenLLM)) {
                        if (logging) Debug.LogWarning("RCC: no LLM API token");
                        goto case RCCMode.Offline;
                    }
                    _apiHeaderLLMAuth = new("Authorization", $"Bearer {_apiTokenLLM}");

                    _apiModelLLM = PlayerPrefs.GetString(_PREFS_KEY_API_MODEL_LLM, "");
                    if (string.IsNullOrWhiteSpace(_apiModelLLM)) {
                        if (logging) Debug.LogWarning("RCC: no LLM API model");
                        goto case RCCMode.Offline;
                    }

                    // Check API reachability
                    Task<WebResult> llmCheck = WebRequest.Get($"{_apiUrlLLM}models/{_apiModelLLM}", null, BuildAPIHeaders(_apiHeaderLLMAuth), _webRequestTimeout);
                    Task<WebResult> ttsCheck = Task.FromResult<WebResult>(default);
                    if (_targetMode == RCCMode.Online) {
                        _apiTokenTTS = PlayerPrefs.GetString(_PREFS_KEY_API_TOKEN_TTS, "");
                        _apiModelTTS = PlayerPrefs.GetString(_PREFS_KEY_API_MODEL_TTS, "");
                        if (!string.IsNullOrWhiteSpace(_apiTokenTTS) && !string.IsNullOrWhiteSpace(_apiModelTTS)) {
                            _apiHeaderTTSAuth = new("xi-api-key", _apiTokenTTS);
                            ttsCheck = WebRequest.Get($"{_apiUrlTTS}user", null, BuildAPIHeaders(_API_HEADER_ACCEPT_JSON, _apiHeaderTTSAuth), _webRequestTimeout);
                        }
                    }

                    // Wait for responses...
                    await Task.WhenAll(llmCheck, ttsCheck);

                    // Final checks
                    if (llmCheck.Result.error) {
                        if (logging) Debug.LogWarning($"RCC: LLM API response error: {llmCheck.Result.errorMsg}");
                        goto case RCCMode.Offline;
                    }
                    if (_targetMode == RCCMode.Online && !string.IsNullOrWhiteSpace(_apiTokenTTS) && !string.IsNullOrWhiteSpace(_apiModelTTS) && !ttsCheck.Result.error) {
                        Mode = RCCMode.Online;
                        if (logging) Debug.Log("RCC: [Online]");
                        FetchTTSVoices();
                        break;
                    } else {
                        Mode = RCCMode.TextOnly;
                        if (logging) {
                            if (_targetMode == RCCMode.Online) {
                                if (string.IsNullOrWhiteSpace(_apiTokenTTS)) Debug.LogWarning("RCC: no TTS API token");
                                else if (string.IsNullOrWhiteSpace(_apiModelTTS)) Debug.LogWarning("RCC: no TTS API model");
                                else if (ttsCheck.Result.error) Debug.LogWarning($"RCC: TTS API response error: {ttsCheck.Result.errorMsg}");
                            }
                            Debug.Log("RCC: [TextOnly]");
                        }
                        break;
                    }
                default:
                    goto case RCCMode.Offline;
            }
        }

        private KeyValuePair<string, string>[] BuildAPIHeaders(params KeyValuePair<string, string>[] headers) {
            KeyValuePair<string, string>[] res = new KeyValuePair<string, string>[headers.Length];
            for (int i = 0; i < res.Length; i++) res[i] = headers[i];
            return res;
        }

        private async void FetchTTSVoices() {
            WebResult webRes = await WebRequest.Get($"{_apiUrlTTS}voices", null, BuildAPIHeaders(_API_HEADER_ACCEPT_JSON, _apiHeaderTTSAuth), _webRequestTimeout);
            if (webRes.error) {
                Debug.LogWarning($"RCC: FetchTTSVoices: TTS API response error: {webRes.errorMsg}");
                return;
            }

            TTSVoicesResponse voicesRes = TTSVoicesResponse.FromJSON(webRes.result);
            if (voicesRes.voices.Length > 0) _TTSVoices.Clear();
            else {
                Debug.LogWarning($"RCC: FetchTTSVoices: no voices returned");
                return;
            }
            foreach (TTSVoice voice in voicesRes.voices) _TTSVoices.Add(voice);
        }

#if ENABLE_DEBUG_FEATURES
        private void ToggleDebugUI() {
            _debugUI.GetComponentInChildren<EventSystem>(true).gameObject.SetActive(EventSystem.current == null);
            _debugUI.gameObject.SetActive(!_debugUI.gameObject.activeSelf);
        }
#endif

#if UNITY_EDITOR
        private RCCMode _oldTargetMode;
        private void OnValidate() {
            if (!Application.isPlaying) return;
            if (_targetMode != _oldTargetMode) {
                _ = ModeUpdate(_modeUpdateLogs);
                _oldTargetMode = _targetMode;
            }
        }
#endif

        public enum RCCMode : byte {
            Offline,
            TextOnly,
            Online
        }
    }
}
