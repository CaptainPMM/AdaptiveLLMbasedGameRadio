using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;
using AdBlocker.FMOD.LLMCommands;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.Radio.RadioContents {
    [System.Serializable]
    public class RadioContent {
        public Exception Exception { get; private set; }

        [field: SerializeField, ReadOnly] public RadioContentType Type { get; private set; }
        [field: SerializeField] public LLMPrompt Prompt { get; private set; }
        [field: SerializeField, ReadOnly] public byte Sections { get; private set; }
        [SerializeField, ReadOnly] private List<RadioSpeaker> _radioSpeakers;
        [SerializeField, Multiline(5), ReadOnly] private List<string> _textContentSections;
        [SerializeField, HideInInspector] private List<byte[]> _audioContentSections;
        [field: SerializeField, HideInInspector] public byte[] ConcatAudioContentSections { get; private set; }

        public ReadOnlyCollection<RadioSpeaker> RadioSpeakers => _radioSpeakers?.AsReadOnly();
        public ReadOnlyCollection<string> TextContentSections => _textContentSections?.AsReadOnly();
        public ReadOnlyCollection<byte[]> AudioContentSections => _audioContentSections?.AsReadOnly();

        /// <summary>
        /// Offline creation.
        /// </summary>
        public RadioContent(RadioContentType type, byte sections, IEnumerable<byte[]> audioContentSections) : this(type, default, sections, null, null, audioContentSections) { }

        /// <summary>
        /// TextOnly creation.
        /// </summary>
        public RadioContent(RadioContentType type, LLMPrompt prompt, byte sections, IEnumerable<RadioSpeaker> radioSpeakers, IEnumerable<string> textContentSections) : this(type, prompt, sections, radioSpeakers, textContentSections, null) { }

        /// <summary>
        /// Online creation.
        /// </summary>
        public RadioContent(RadioContentType type, LLMPrompt prompt, byte sections, IEnumerable<RadioSpeaker> radioSpeakers, IEnumerable<string> textContentSections, IEnumerable<byte[]> audioContentSections) {
            Type = type;
            Prompt = prompt;
            Sections = sections;
            _radioSpeakers = radioSpeakers != null ? new(radioSpeakers) : new();
            _textContentSections = textContentSections != null ? new(textContentSections) : new();
            _audioContentSections = audioContentSections != null ? new(audioContentSections) : new();
            ConcatenateAudioContentSections();
        }

        /// <summary>
        /// Invalid content creation.
        /// Exception is set.
        /// </summary>
        public RadioContent(Exception exception) : this(RadioContentType.Missing, default, 0, null, null, null) {
            Exception = exception;
        }

        public bool HasException(out Exception exception) {
            exception = Exception;
            return exception != null;
        }

        public RadioContent TextOnlyToOnlineUpgrade(IEnumerable<byte[]> audioContentSections) {
            if (audioContentSections != null) _audioContentSections = new(audioContentSections);
            else Debug.LogWarning("RadioContent: TextOnlyToOnlineUpgrade: audio content sections are null");
            if (Sections != _audioContentSections.Count) Debug.LogWarning("RadioContent: TextOnlyToOnlineUpgrade: audio content sections not equal to radio content sections");
            ConcatenateAudioContentSections();
            return this;
        }

        private void ConcatenateAudioContentSections() {
            uint byteSize = 0;
            foreach (byte[] audioContentSection in _audioContentSections) byteSize += (uint)audioContentSection.Length;

            ConcatAudioContentSections = new byte[byteSize];

            byteSize = 0;
            foreach (byte[] audioContentSection in _audioContentSections) {
                Array.Copy(audioContentSection, 0, ConcatAudioContentSections, byteSize, audioContentSection.Length);
                byteSize += (uint)audioContentSection.Length;
            }
        }

        [System.Serializable]
        public class RCException : Exception {
            public RCException() { }
            public RCException(string message) : base(message) { }
            public RCException(string message, Exception inner) : base(message, inner) { }
            protected RCException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}