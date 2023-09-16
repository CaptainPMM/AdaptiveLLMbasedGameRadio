using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using AdBlocker.FMOD.GameStates.Values;

namespace AdBlocker.FMOD.LLMCommands {
    [System.Serializable]
    public class LLMCommandTrigger {
        private static readonly Dictionary<LLMCommandTriggerType, byte> _typeToNumParams = new() {
            { LLMCommandTriggerType.Never, 0 },
            { LLMCommandTriggerType.Always1, 0 },
            { LLMCommandTriggerType.Always100, 0 },
            { LLMCommandTriggerType.Static, 1 },
            { LLMCommandTriggerType.ReputationAscend, 4 }
        };
        public static ReadOnlyDictionary<LLMCommandTriggerType, byte> TypeToNumParams => new(_typeToNumParams);

        [field: SerializeField] public LLMCommandType CmdType { get; private set; }
        [field: SerializeField] public LLMCommandTriggerType TriggerType { get; private set; } = LLMCommandTriggerType.Always100;
        [field: SerializeField] public List<string> Parameters { get; private set; }

        /// <summary>
        /// Validate (and set) the parameters count to the correct size based on the type.
        /// </summary>
        /// <returns>True, if the size was correct. False, if the size had to be corrected</returns>
        public bool ValidateParameters() {
            if (Parameters.Count == _typeToNumParams[TriggerType]) return true;
            else {
                int countDiff = Parameters.Count - _typeToNumParams[TriggerType];
                if (countDiff > 0) Parameters.RemoveRange(_typeToNumParams[TriggerType], countDiff);
                else if (countDiff < 0) for (int i = 0; i < Mathf.Abs(countDiff); i++) Parameters.Add("");
                return false;
            }
        }

        /// <summary>
        /// Get the trigger probability [0,1].
        /// </summary>
        /// <returns>trigger probability [0,1]</returns>
        public float GetProbability() {
            if (!ValidateParameters()) {
                Debug.LogWarning($"LLMCommandTrigger: type <{TriggerType}> requires {_typeToNumParams[TriggerType]} parameters");
                return 0f;
            }

            switch (TriggerType) {
                case LLMCommandTriggerType.Never:
                    return Never();
                case LLMCommandTriggerType.Always1:
                    return Always1();
                case LLMCommandTriggerType.Always100:
                    return Always100();
                case LLMCommandTriggerType.Static:
                    return Static();
                case LLMCommandTriggerType.ReputationAscend:
                    return ReputationAscend();
                default:
                    goto case LLMCommandTriggerType.Never;
            }
        }

        private float Never() {
            return 0f;
        }

        private float Always1() {
            return 1f;
        }

        private float Always100() {
            return 100f;
        }

        private float Static() {
            return float.Parse(Parameters[0]);
        }

        private float ReputationAscend() {
            switch (FMODManagers.GameStateExtractor.GetGameStateValueObj<GVPlayerReputation>().Value) {
                case GVPlayerReputation.PlayerReputation.Unknown:
                    return float.Parse(Parameters[0]);
                case GVPlayerReputation.PlayerReputation.Noticed:
                    return float.Parse(Parameters[1]);
                case GVPlayerReputation.PlayerReputation.Named:
                    return float.Parse(Parameters[2]);
                case GVPlayerReputation.PlayerReputation.Famous:
                    return float.Parse(Parameters[3]);
                default:
                    goto case GVPlayerReputation.PlayerReputation.Unknown;
            }
        }
    }
}