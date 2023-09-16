namespace AdBlocker.FMOD.Radio.RadioContents.APIModels.TTS {
    [System.Serializable]
    public struct TTSVoice {
        public string voice_id;
        public string name;

        public TTSVoice(string voice_id, string name) {
            this.voice_id = voice_id;
            this.name = name;
        }
    }
}