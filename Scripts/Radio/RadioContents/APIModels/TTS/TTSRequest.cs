using UnityEngine;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;

namespace AdBlocker.FMOD.Radio.RadioContents.APIModels.TTS {
    [System.Serializable]
    public struct TTSRequest : IAPIRequest {
        public SpeakerVoiceSettings voice_settings;
        public string model_id;
        public string text;

        public TTSRequest(SpeakerVoiceSettings voice_settings, string model_id, string text) {
            this.voice_settings = voice_settings;
            this.model_id = model_id;
            this.text = text;
        }

        public string ToJSON() {
            return JsonUtility.ToJson(this);
        }
    }
}