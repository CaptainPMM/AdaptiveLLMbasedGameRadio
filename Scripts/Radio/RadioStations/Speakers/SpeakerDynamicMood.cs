using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using AdBlocker.FMOD.GameStates.Values;

namespace AdBlocker.FMOD.Radio.RadioStations.Speakers {
    [System.Serializable]
    public class SpeakersDynamicMood {
        private static readonly Dictionary<SpeakerDynamicMoodType, byte> _typeToNumParams = new() {
            { SpeakerDynamicMoodType.StaticOnly, 0 },
            { SpeakerDynamicMoodType.ReputationAscendPanic, 4 }
        };
        public static ReadOnlyDictionary<SpeakerDynamicMoodType, byte> TypeToNumParams => new(_typeToNumParams);

        [field: SerializeField] public SpeakerDynamicMoodType Type { get; private set; } = SpeakerDynamicMoodType.StaticOnly;
        [field: SerializeField] public List<string> Parameters { get; private set; }

        /// <summary>
        /// Validate (and set) the parameters count to the correct size based on the type.
        /// </summary>
        /// <returns>True, if the size was correct. False, if the size had to be corrected</returns>
        public bool ValidateParameters() {
            if (Parameters.Count == _typeToNumParams[Type]) return true;
            else {
                int countDiff = Parameters.Count - _typeToNumParams[Type];
                if (countDiff > 0) Parameters.RemoveRange(_typeToNumParams[Type], countDiff);
                else if (countDiff < 0) for (int i = 0; i < Mathf.Abs(countDiff); i++) Parameters.Add("");
                return false;
            }
        }

        /// <summary>
        /// Get the dynamic mood (or static depending on the type).
        /// </summary>
        /// <param name="staticMood">static mood as a base</param>
        /// <returns>dynamic/static mood</returns>
        public string Get(string staticMood) {
            if (!ValidateParameters()) {
                Debug.LogWarning($"SpeakerDynamicMood: type <{Type}> requires {_typeToNumParams[Type]} parameters");
                return staticMood;
            }

            switch (Type) {
                case SpeakerDynamicMoodType.StaticOnly:
                    return StaticOnly(staticMood);
                case SpeakerDynamicMoodType.ReputationAscendPanic:
                    return ReputationAscendPanic(staticMood);
                default:
                    goto case SpeakerDynamicMoodType.StaticOnly;
            }
        }

        private string StaticOnly(string staticMood) {
            return staticMood;
        }

        private string ReputationAscendPanic(string staticMood) {
            switch (FMODManagers.GameStateExtractor.GetGameStateValueObj<GVPlayerReputation>().Value) {
                case GVPlayerReputation.PlayerReputation.Unknown:
                    return StaticCheck(staticMood, Parameters[0]);
                case GVPlayerReputation.PlayerReputation.Noticed:
                    return StaticCheck(staticMood, Parameters[1]);
                case GVPlayerReputation.PlayerReputation.Named:
                    return StaticCheck(staticMood, Parameters[2]);
                case GVPlayerReputation.PlayerReputation.Famous:
                    return StaticCheck(staticMood, Parameters[3]);
                default:
                    goto case GVPlayerReputation.PlayerReputation.Unknown;
            }
        }

        private string StaticCheck(string staticMood, string dynamicMood) {
            return dynamicMood.Replace("static", staticMood);
        }
    }
}