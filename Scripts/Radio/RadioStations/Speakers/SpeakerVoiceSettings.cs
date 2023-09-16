using UnityEngine;

namespace AdBlocker.FMOD.Radio.RadioStations.Speakers {
    [System.Serializable]
    public struct SpeakerVoiceSettings {
        public const float DEFAULT_STABILITY = 0.1f;
        public const float DEFAULT_SIM_BOOST = 0.9f;

        [Range(0f, 1f)] public float stability;
        [Range(0f, 1f)] public float similarity_boost;

        public SpeakerVoiceSettings(float stability = DEFAULT_STABILITY, float similarityBoost = DEFAULT_SIM_BOOST) {
            this.stability = stability;
            this.similarity_boost = similarityBoost;
        }
    }
}