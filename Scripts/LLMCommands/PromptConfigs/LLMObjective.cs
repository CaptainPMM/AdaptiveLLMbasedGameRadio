using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.LLMCommands.PromptConfigs {
    [System.Serializable]
    public class LLMObjective {
        private static readonly Regex _VAR_REGEX = new Regex("{([^{}]+)}", RegexOptions.Multiline);

        [SerializeField, Multiline(4)] private string _objective;
        [SerializeField] private List<KVPair<string, List<WeightedValue<string>>>> Variables;

        public string Get() {
            return _VAR_REGEX.Replace(_objective, match => {
                int kvIndex = Variables.FindIndex(kv => kv.key == match.Groups[1].Value);
                if (kvIndex > -1) {
                    return WeightedValue<string>.GetWeightedRandom(Variables[kvIndex].value);
                }
                return match.Value; // variable not found, return the original match
            });
        }
    }
}