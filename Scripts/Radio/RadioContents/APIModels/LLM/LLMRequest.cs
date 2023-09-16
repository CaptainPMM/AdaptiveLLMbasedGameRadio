using UnityEngine;

namespace AdBlocker.FMOD.Radio.RadioContents.APIModels.LLM {
    [System.Serializable]
    public struct LLMRequest : IAPIRequest {
        public const float DEFAULT_TEMPERATURE = 1f;
        public const uint DEFAULT_MAX_TOKENS = 512;

        public string model;
        public Message[] messages;
        public float temperature;
        public uint max_tokens;

        public LLMRequest(string model, Message[] messages, float temperature = DEFAULT_TEMPERATURE, uint max_tokens = DEFAULT_MAX_TOKENS) {
            this.model = model;
            this.messages = messages;
            this.temperature = temperature;
            this.max_tokens = max_tokens;
        }

        public string ToJSON() {
            return JsonUtility.ToJson(this);
        }

        [System.Serializable]
        public struct Message {
            public string role;
            public string content;

            public Message(Role role, string content) {
                switch (role) {
                    case Role.system:
                        this.role = "system";
                        break;
                    case Role.user:
                        this.role = "user";
                        break;
                    case Role.assistant:
                        this.role = "assistant";
                        break;
                    default:
                        this.role = "undefined";
                        break;
                }
                this.content = content;
            }

            public enum Role : byte {
                system, // context
                user, // command
                assistant // response
            }
        }
    }
}