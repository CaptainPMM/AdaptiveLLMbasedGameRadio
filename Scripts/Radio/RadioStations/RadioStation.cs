using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using FMODUnity;
using AdBlocker.FMOD.Radio.RadioContents;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;
using AdBlocker.FMOD.LLMCommands;
using AdBlocker.Utils;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace AdBlocker.FMOD.Radio.RadioStations {
    [CreateAssetMenu(fileName = "Radio Station", menuName = "FMOD/Radio/Radio Station", order = 1000_1)]
    public class RadioStation : ScriptableObject {
        [field: Header("General")]
        [field: SerializeField] public RadioStationType Type { get; private set; }
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField, Multiline(5)] public string StationCtx { get; private set; } = FMODDefaults.Radio.Station.StationCtx.CORPS_POSITIVE;

        [Header("Speakers")]
        [SerializeField] private List<WeightedValue<KVPair<SpeakerRole, RadioSpeaker>>> _speakers;

        [Header("Radio Content")]
        [SerializeField] private List<KVPair<RadioContentType, List<LLMCommandTrigger>>> _LLMCommandTriggers;
        [SerializeField] private List<WeightedValue<LLMCommand>> _LLMCommands;

        [Header("FMOD")]
        [SerializeField, BankRef] private List<string> _radioBanks;
        public ReadOnlyCollection<string> RadioBanks => _radioBanks.AsReadOnly();

        [field: SerializeField] public EventReference RadioEvent { get; private set; }

        /// <summary>
        /// Get a radio speaker with the designated role. Returns null if the role is not present.
        /// </summary>
        /// <param name="role">desired role</param>
        /// <returns>radio speaker data</returns>
        public RadioSpeaker GetRadioSpeaker(SpeakerRole role) {
            return WeightedValue<KVPair<SpeakerRole, RadioSpeaker>>.GetWeightedRandom(_speakers, (e) => e.value.key == role).value;
        }

        /// <summary>
        /// Get a LLMCommand of a designated radio content type. Returns null if there is no fitting command for this radio station.
        /// Radio content type (given) -> LLM command type (random based on triggers for same radio content type) -> LLM command instance (random based on weights)
        /// </summary>
        /// <param name="contentType">desired content type</param>
        /// <returns>LLMCommand of the desired content type</returns>
        public LLMCommand GetLLMCommand(RadioContentType contentType) {
            LLMCommandType cmdType = RadioContentToLLMCommandType(contentType);
            return WeightedValue<LLMCommand>.GetWeightedRandom(_LLMCommands, (e) => e.value.CmdType == cmdType);
        }

        /// <summary>
        /// Get a LLMCommand of a designated command type. Returns null if there is no fitting command for this radio station.
        /// </summary>
        /// <param name="cmdType">desired command type</param>
        /// <returns>LLMCommand of the desired command type</returns>
        public LLMCommand GetLLMCommand(LLMCommandType cmdType) {
            return WeightedValue<LLMCommand>.GetWeightedRandom(_LLMCommands, (e) => e.value.CmdType == cmdType);
        }

        public List<WeightedValue<LLMCommand>> GetLLMCommands(LLMCommandType cmdType) {
            List<WeightedValue<LLMCommand>> cmds = new();
            foreach (var wvLLM in _LLMCommands) {
                if (wvLLM.value.CmdType == cmdType) cmds.Add(wvLLM);
            }
            return cmds;
        }

        private LLMCommandType RadioContentToLLMCommandType(RadioContentType contentType) {
            List<LLMCommandTrigger> cmdTriggers = _LLMCommandTriggers.Find((kv) => kv.key == contentType).value;
            if (cmdTriggers != null) {
                float probSum = 0f;
                foreach (var cmdTrigger in cmdTriggers) {
                    probSum += Mathf.Abs(cmdTrigger.GetProbability());
                }

                float rand = Random.Range(0f, probSum);
                probSum = 0f;
                foreach (var cmdTrigger in cmdTriggers) {
                    probSum += cmdTrigger.GetProbability();
                    if (rand < probSum) return cmdTrigger.CmdType;
                }
            }
            return LLMCommandType.Missing;
        }

#if UNITY_EDITOR
        [OnOpenAssetAttribute]
        private static bool EditorOpenAsset(int instanceID, int line) {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj.GetType() == typeof(RadioStation)) {
                EditorUtility.OpenPropertyEditor(obj);
                return true;
            } else return false;
        }

        private void OnValidate() {
            foreach (var kv in _LLMCommandTriggers) {
                foreach (var cmdTrigger in kv.value) {
                    cmdTrigger.ValidateParameters();
                }
            }
        }
#endif
    }
}