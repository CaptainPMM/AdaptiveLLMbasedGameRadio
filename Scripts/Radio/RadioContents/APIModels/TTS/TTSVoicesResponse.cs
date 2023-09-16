using UnityEngine;

namespace AdBlocker.FMOD.Radio.RadioContents.APIModels.TTS {
    [System.Serializable]
    public struct TTSVoicesResponse {
        public TTSVoice[] voices;

        public TTSVoicesResponse(TTSVoice[] voices) {
            this.voices = voices;
        }

        public static TTSVoicesResponse FromJSON(string json) {
            return JsonUtility.FromJson<TTSVoicesResponse>(json);
        }
    }
}