using UnityEngine;
using AdBlocker.FMOD.Radio.RadioContents;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.LLMCommands {
    [System.Serializable]
    public struct LLMPrompt {
        [ReadOnly] public RadioContentType contentType;
        [ReadOnly] public LLMCommandType cmdType;
        [Multiline(5)] public string system;
        [Multiline(5)] public string command;
        [Multiline(2)] public string outputParams;
        [Multiline(8)] public string fullTxt;

        public LLMPrompt(RadioContentType contentType, LLMCommandType cmdType, string system, string command, string outputParams) {
            this.contentType = contentType;
            this.cmdType = cmdType;
            this.system = system;
            this.command = command;
            this.outputParams = outputParams;

            TextBuilder fullTxtBuilder = new();
            fullTxtBuilder.AddLine($"[[{cmdType.ToString().ToUpper()}]]");
            if (!string.IsNullOrWhiteSpace(system)) {
                fullTxtBuilder.AddLine($"[SYSTEM]");
                fullTxtBuilder.AddLine(system);
            }
            if (!string.IsNullOrWhiteSpace(command)) {
                fullTxtBuilder.AddLine($"\n[COMMAND]");
                fullTxtBuilder.AddLine(command);
            }
            if (!string.IsNullOrWhiteSpace(outputParams)) {
                fullTxtBuilder.AddLine($"\n[OUTPUTPARAMS]");
                fullTxtBuilder.AddLine(outputParams);
            }
            this.fullTxt = fullTxtBuilder.Text;
        }

        public override string ToString() {
            return fullTxt;
        }
    }
}