using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using AdBlocker.FMOD.GameStates.Values;
using AdBlocker.FMOD.GameStates.Events;
using AdBlocker.World;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.GameStates {
    public class GameStateExtractor : MonoBehaviour {
        [Header("Settings")]
        [SerializeField, Min(0f), Tooltip("in seconds")] private float _updateInterval = 10f;

        [Header("Game State Values")]
        [SerializeReference]
        private List<GameStateValue> _gameStateValues = new() {
            // The order matters for the DB route and update value dependencies.
            // In case the state value is used as game context in LLMCommands add handling to the RadioAudioDBManager.
            new GVPlayerProgression(),
            new GVPlayerReputation(),
            new GVIntensity()
        };
        public ReadOnlyCollection<GameStateValue> GameStateValues => _gameStateValues.AsReadOnly();

        [Header("Events Chain")]
        [SerializeReference]
        private List<GameStateEvent> _eventsChain = new();
        public ReadOnlyCollection<GameStateEvent> EventsChain => _eventsChain.AsReadOnly();

        private Dictionary<System.Type, GameStateValue> _gameStateValuesDict;
        public ReadOnlyDictionary<System.Type, GameStateValue> GameStateValuesDict { get; private set; }

        public delegate void GSValuesUpdated(ReadOnlyCollection<GameStateValue> values);
        public event GSValuesUpdated OnGSValuesUpdate;

        public delegate void GSEventAdded(GameStateEvent ev);
        public event GSEventAdded OnEventAdded;

        private void Awake() {
            BuildGameStateValuesDict();
        }

        private void Start() {
            RegisterEvents();
            StartCoroutine(UpdateValuesRoutine());
        }

        private void BuildGameStateValuesDict() {
            _gameStateValuesDict = new();
            foreach (var gv in _gameStateValues) _gameStateValuesDict.Add(gv.GetType(), gv);
            GameStateValuesDict = new(_gameStateValuesDict);
        }

        private void RegisterEvents() {
            if (!Game.Game.Instance || !Game.Game.Instance.SurveillanceSystem) {
                Debug.LogWarning("GameStateExtractor: no game instance or surveillance system found");
                return;
            }

            // District type is currently hard coded, because we will only have one district for now...
            Game.Game.Instance.SurveillanceSystem.OnPlayerSighted += (sender, args) => AddEvent(new GEChase(args.Cause, DistrictType.NewHaven));
            Game.Game.Instance.SurveillanceSystem.OnPlayerEscape += (sender, args) => AddEvent(new GEEscape(DistrictType.NewHaven));
            Game.Game.Instance.SurveillanceSystem.OnPlayerKilled += (sender, args) => AddEvent(new GECapture(DistrictType.NewHaven));
        }

        private IEnumerator UpdateValuesRoutine() {
            while (this) { // as long as the object exists
                UpdateValues();
                yield return new WaitForSeconds(_updateInterval);
            }
        }

        public void UpdateValues() {
            _gameStateValues.ForEach(gsv => gsv.Update());
            OnGSValuesUpdate?.Invoke(GameStateValues);
        }

        public void AddEvent(GameStateEvent ev) {
            _eventsChain.Add(ev);
            OnEventAdded?.Invoke(ev);
        }

        /// <summary>
        /// Get the current game state as textual representation.
        /// </summary>
        /// <param name="valueTypes">game state value types to include (null to get all values)</param>
        /// <param name="numEvents">number of events to include (-1 to get all events recorded)</param>
        /// <param name="onlyRecentEvents">false distributes the number of events over all events from the beginning</param>
        /// <returns>game state as text</returns>
        public string GetGameStateText(IEnumerable<GSValueType> valueTypes = null, int numEvents = -1, bool onlyRecentEvents = true) {
            TextBuilder gs = new();

            foreach (GameStateValue gsv in _gameStateValues) {
                if (valueTypes != null && !valueTypes.Contains(gsv.Type)) continue;
                string text = gsv.ConvertToText();
                if (!string.IsNullOrWhiteSpace(text)) gs.AddLine(text);
            }

            if (numEvents < 0) numEvents = _eventsChain.Count;
            if (numEvents > _eventsChain.Count) numEvents = _eventsChain.Count;
            if (numEvents > 0) {
                gs.AddLine("\nPlayer's recent activity from oldest to newest:");
                if (onlyRecentEvents) {
                    for (int i = numEvents; i > 0; i--) {
                        gs.AddLine($"{numEvents - i + 1}. {_eventsChain[^i].ConvertToText()}");
                    }
                } else {
                    // Distribute number of events over all events from the beginning
                    int order = 1;
                    for (float f = 0f; f < (float)_eventsChain.Count; f += (float)_eventsChain.Count / (float)numEvents) {
                        gs.AddLine($"{order++}. {_eventsChain[(int)f].ConvertToText()}");
                    }
                }
            }

            return gs.Text;
        }

        /// <summary>
        /// Get the current game state as DB route representation.
        /// </summary>
        /// <param name="numRecentEvents">number of recent events (-1 to get all events recorded)</param>
        /// <returns>game state as DB route</returns>
        public string GetDBRoute(int numRecentEvents) {
            TextBuilder route = new();

            _gameStateValues.ForEach(gsv => route.Add(!string.IsNullOrWhiteSpace(gsv.DBKey) ? gsv.DBRoute : ""));

            if (numRecentEvents < 0) numRecentEvents = _eventsChain.Count;
            if (numRecentEvents > _eventsChain.Count) numRecentEvents = _eventsChain.Count;
            for (int i = numRecentEvents; i > 0; i--) {
                route.Add(_eventsChain[^i].DBRoute);
            }

            return route.Text;
        }

        /// <summary>
        /// Returns the object behind a GameStateValue type.
        /// </summary>
        /// <typeparam name="T">generic GameStateValue type</typeparam>
        /// <returns>GameStateValue type object</returns>
        public T GetGameStateValueObj<T>() where T : GameStateValue {
            return (T)_gameStateValuesDict[typeof(T)];
        }

        /// <summary>
        /// Override a game state value with a specified type.
        /// </summary>
        /// <param name="type">Game State Value type</param>
        /// <param name="newValue">new Game State Value to insert for the old one</param>
        public void OverrideGameStateValue(GSValueType type, GameStateValue newValue) {
            int index = _gameStateValues.FindIndex(gsv => gsv.Type == type);
            _gameStateValues[index] = newValue;
            BuildGameStateValuesDict();
        }
    }
}
