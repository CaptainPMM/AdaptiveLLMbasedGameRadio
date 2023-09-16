using System.Collections.Generic;
using UnityEngine;
using AdBlocker.FMOD.Radio.RadioContents;
using AdBlocker.FMOD.GameStates.Values;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.LLMCommands.PromptConfigs {
    [System.Serializable]
    public class LLMFormat {
        [SerializeField] private bool _useBaseFormat = true;
        [SerializeField, Min(-1)] private int _wordLength = FMODDefaults.LLMCommand.Prompt.Format.Length.LONG;
        [SerializeField] private bool _usePlayerAlias = true;
        [SerializeField] private string _introductionTo = "";
        [SerializeField] private bool _adsComingUpNext = true;
        [SerializeField] private List<string> _customFormats;

        public string Get(int numSpeakers) {
            TextBuilder tb = new();
            if (_useBaseFormat) tb.AddLine($"* the output must match the regex {RadioContentCreator.LLM_TEXT_VALIDATION_REGEX_STRING} where for each speaker paragraph a speaker name and speaker text group can be extracted")
                                  .AddLine($"* the output must have exactly {numSpeakers} speaker paragraph" + (numSpeakers > 1 ? "s" : ""))
                                  .AddLine($"* speaker text must not contain new lines, line breaks or '\\n'")
                                  .AddLine("* use expressive language");
            if (_wordLength > -1) tb.AddLine($"* the output should have {_wordLength} words (excluding output parameters)");
            if (_usePlayerAlias) {
                string playerAlias = GVPlayerReputation.ReputationToPlayerAlias[FMODManagers.GameStateExtractor.GetGameStateValueObj<GVPlayerReputation>().Value];
                tb.AddLine($"* the player name/alias is {playerAlias}");
            }
            if (!string.IsNullOrWhiteSpace(_introductionTo)) tb.AddLine($"* speaker text should start with an introductory sentence leading to the {_introductionTo}");
            if (_adsComingUpNext) tb.AddLine("* speaker text should end with a transition to the radio ads section coming up next");
            foreach (string format in _customFormats) tb.AddLine($"* {format}");
            return tb.Text;
        }
    }
}