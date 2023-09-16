using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.InputSystem;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using AdBlocker.FMOD.Radio.RadioStations;
using AdBlocker.FMOD.Radio.RadioContents;
using AdBlocker.FMOD.GameStates.Values;
using AdBlocker.FMOD.GameStates.Events;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.Radio {
    public partial class RadioManager : MonoBehaviour {
        [Header("Setup/FMOD/Banks")]
        [SerializeField, BankRef, Setup] private List<string> _generalRadioBanks;

        [Header("Setup/FMOD/Events")]
        [SerializeField] private EventReference _eventRadioTune;

        [Header("Setup/FMOD/Params")]
        [SerializeField, ParamRef, Setup] private string _fmodParamStage;
        [SerializeField, ParamRef, Setup] private string _fmodParamIntensity;
        [SerializeField, ParamRef, Setup] private string _fmodParamMusicOffset;

        [Header("Setup/Radio")]
        [SerializeField] private List<RadioStation> _radioStations;

        [Header("Settings")]
        [SerializeField] private RadioStationType _activeRadioStation;
        public RadioStationType ActiveRadioStation => _activeRadioStation;

        [field: Header("Runtime")]
        [field: SerializeField, ReadOnly] public RadioContentType CurrentlyPlaying { get; private set; }

        [Header("Radio Content")]
        [SerializeField, ReadOnly] private bool _waitingForRadioContent = false;
        [SerializeField] private List<RadioContent> _radioLoopContents = new();

        private static Queue<Action> _mainThreadActions = new();
        private static EVENT_CALLBACK _radioContentCallback;

        private Dictionary<RadioStationType, RadioStation> _radioStationsDict;
        public ReadOnlyDictionary<RadioStationType, RadioStation> RadioStationsDict => new(_radioStationsDict);

        private EventInstance _evInstRadioTune;

        private void Awake() {
            foreach (string bank in _generalRadioBanks) RuntimeManager.LoadBank(bank);

            _radioStationsDict = new();
            foreach (RadioStation rs in _radioStations) _radioStationsDict.Add(rs.Type, rs);

#if UNITY_EDITOR
            _oldActiveRadioStation = _activeRadioStation;
#endif
        }

        private void Start() {
            FMODManagers.GameStateExtractor.OnGSValuesUpdate += GameStateValuesUpdateHandler;
            FMODManagers.GameStateExtractor.OnEventAdded += GameStateEventAddedHandler;

            _radioContentCallback = new EVENT_CALLBACK(RadioContentCallbackHandler);

            StartActiveRadio();
        }

        private void Update() {
            UpdateCurrentlyPlaying();
            RunMainThreadActions();

#if ENABLE_DEBUG_FEATURES
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.digit5Key.wasPressedThisFrame)
                SetActiveRadioStation((RadioStationType)(((byte)_activeRadioStation + 1) % System.Enum.GetValues(typeof(RadioStationType)).Length), true);
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.digit6Key.wasPressedThisFrame)
                CreateRadioLoopContent();
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.f9Key.isPressed && Keyboard.current.digit7Key.wasPressedThisFrame)
                NextFMODStage();
#endif
        }

        public void SetActiveRadioStation(RadioStationType type, bool randMusicOffset = false) {
            if (type == _activeRadioStation) return;

            FMODManagers.Helper.StartEvent(_eventRadioTune, out _evInstRadioTune);

            StopActiveRadio();

            _activeRadioStation = type;
#if UNITY_EDITOR
            _oldActiveRadioStation = _activeRadioStation;
#endif

            SetCurrentlyPlaying(RadioContentType.Music);
            RuntimeManager.StudioSystem.setParameterByName(_fmodParamMusicOffset, (randMusicOffset ? 1f : 0f) * UnityEngine.Random.value * 0.5f);

            StartActiveRadio();
        }

        /// <summary>
        /// Get the currently active radio station. Returns null if no station is active.
        /// </summary>
        /// <returns>active radio station</returns>
        public RadioStation GetActiveRadioStationData() {
            if (_activeRadioStation == RadioStationType.None) return null;
            return _radioStationsDict[_activeRadioStation];
        }

        private void UpdateCurrentlyPlaying() {
            if (RuntimeManager.StudioSystem.getParameterByName(_fmodParamStage, out _, out float stage) == RESULT.OK)
                CurrentlyPlaying = (RadioContentType)(stage + 1f);
            else CurrentlyPlaying = RadioContentType.Missing;
        }

        private void SetCurrentlyPlaying(RadioContentType type) {
            switch (type) {
                case RadioContentType.Missing:
                case RadioContentType.BreakingNews:
                case RadioContentType.Comments:
                    UnityEngine.Debug.LogWarning($"RadioManager: type <{type}> not supported");
                    return;
            }
            RuntimeManager.StudioSystem.setParameterByName(_fmodParamStage, (float)type - 1f);
        }

        private void NextFMODStage() {
            SetCurrentlyPlaying((RadioContentType)((byte)CurrentlyPlaying % 3) + 1); // ignore Missing, Breaking News and Comments
        }

        private void RunMainThreadActions() {
            if (_mainThreadActions.Count > 0) {
                int numActions = _mainThreadActions.Count;
                while (numActions > 0) {
                    _mainThreadActions.Dequeue().Invoke();
                    numActions--;
                }
            }
        }

        [ContextMenu("Start Active Radio")]
        private void StartActiveRadio() {
            if (_activeRadioStation != RadioStationType.None) {
                RadioStation station = GetActiveRadioStationData();
                foreach (string bank in station.RadioBanks) RuntimeManager.LoadBank(bank);
                StartCoroutine(AwaitBanksAndStartEvent(station));
            } else FMODManagers.Helper.StopEvent(_evInstRadioTune);

            IEnumerator AwaitBanksAndStartEvent(RadioStation station) {
                yield return new WaitUntil(() => RuntimeManager.HaveAllBanksLoaded);

                var evDesc = RuntimeManager.GetEventDescription(station.RadioEvent);
                evDesc.createInstance(out var evInstance);
                evInstance.setCallback(_radioContentCallback);
                evInstance.start();
                evInstance.release();

                FMODManagers.Helper.StopEvent(_evInstRadioTune);
                RuntimeManager.StudioSystem.setParameterByName(_fmodParamMusicOffset, 0f);
            }
        }

        [ContextMenu("Stop Active Radio")]
        private void StopActiveRadio() {
            if (_activeRadioStation != RadioStationType.None) {
                RadioStation station = GetActiveRadioStationData();
                try { RuntimeManager.GetEventDescription(station.RadioEvent).releaseAllInstances(); } catch { } // event may not be loaded yet
                foreach (string bank in station.RadioBanks) RuntimeManager.UnloadBank(bank);

                _activeRadioStation = RadioStationType.None;
#if UNITY_EDITOR
                _oldActiveRadioStation = _activeRadioStation;
#endif
            }
        }

        private void GameStateValuesUpdateHandler(ReadOnlyCollection<GameStateValue> values) {
            foreach (GameStateValue gsv in values) {
                switch (gsv.Type) {
                    case GSValueType.Intensity:
                        RuntimeManager.StudioSystem.setParameterByName(_fmodParamIntensity, (float)((GVIntensity)gsv).Value);
                        break;
                }
            }
        }

        private void GameStateEventAddedHandler(GameStateEvent ev) {
            // TODO trigger FMOD event
            // TODO prevent wrong triggering of interruptions like breaking news not double or comments during news etc.
        }

        private void OnDestroy() {
            FMODManagers.GameStateExtractor.OnGSValuesUpdate -= GameStateValuesUpdateHandler;
            FMODManagers.GameStateExtractor.OnEventAdded -= GameStateEventAddedHandler;
        }

#if UNITY_EDITOR
        [ContextMenu("Create Radio Loop Content")]
        private void EditorCreateRadioLoopContent() => CreateRadioLoopContent();

        [ContextMenu("Play Music")]
        private void EditorPlayMusic() {
            SetCurrentlyPlaying(RadioContentType.Music);
        }

        [ContextMenu("Play News")]
        private void EditorPlayNews() {
            SetCurrentlyPlaying(RadioContentType.News);
        }

        [ContextMenu("Play Ads")]
        private void EditorPlayAds() {
            SetCurrentlyPlaying(RadioContentType.Ads);
        }

        private RadioStationType _oldActiveRadioStation;
        private void OnValidate() {
            if (!Application.isPlaying) return;
            if (_activeRadioStation != _oldActiveRadioStation) {
                if (_oldActiveRadioStation != RadioStationType.None && _radioStationsDict.TryGetValue(_oldActiveRadioStation, out RadioStation station)) {
                    try { RuntimeManager.GetEventDescription(station.RadioEvent).releaseAllInstances(); } catch { } // event may not be loaded yet
                    foreach (string bank in station.RadioBanks) RuntimeManager.UnloadBank(bank);
                }
                StartActiveRadio();
                _oldActiveRadioStation = _activeRadioStation;
            }
        }
#endif
    }
}