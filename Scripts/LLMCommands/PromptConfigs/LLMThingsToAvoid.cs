using System.Collections.Generic;
using UnityEngine;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.LLMCommands.PromptConfigs {
    [System.Serializable]
    public class LLMThingsToAvoid {
        [SerializeField] private bool _avoidTemplateStrings = true;
        [SerializeField] private bool _avoidCompanyNegatives = true;
        [SerializeField] private bool _avoidPlayerTerm = true;
        [SerializeField] private List<string> _avoidTerms;

        public string Get() {
            TextBuilder tb = new();
            if (_avoidTemplateStrings) tb.AddLine("* avoid template strings but use proper values");
            if (_avoidCompanyNegatives) tb.AddLine("* avoid negative opinions about companies");
            if (_avoidPlayerTerm) tb.AddLine("* avoid the word 'player' but use his alias");
            foreach (string term in _avoidTerms) tb.AddLine($"* {term}");
            return tb.Text;
        }
    }
}