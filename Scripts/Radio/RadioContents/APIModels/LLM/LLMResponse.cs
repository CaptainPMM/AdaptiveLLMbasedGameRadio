using UnityEngine;

namespace AdBlocker.FMOD.Radio.RadioContents.APIModels.LLM {
    [System.Serializable]
    public struct LLMResponse {
        public Choice[] choices;

        public LLMResponse(Choice[] choices) {
            this.choices = choices;
        }

        public static LLMResponse FromJSON(string json) {
            return JsonUtility.FromJson<LLMResponse>(json);
        }

        /// <summary>
        /// Returns the important content from the LLM response or null/"" if something went wrong.
        /// </summary>
        /// <returns>important content from the LLM response or null/"" if something went wrong</returns>
        public string GetContent() {
            // Only one choice set for now
            if (choices.Length != 1) return null;
            return choices[0].message.content;
        }

        [System.Serializable]
        public struct Choice {
            public Message message;

            public Choice(Message message) {
                this.message = message;
            }

            [System.Serializable]
            public struct Message {
                public string content;

                public Message(string content) {
                    this.content = content;
                }
            }
        }
    }
}