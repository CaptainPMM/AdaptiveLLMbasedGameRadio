using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace AdBlocker.FMOD.Radio.RadioStations.Speakers {
    [CreateAssetMenu(fileName = "Radio Speaker", menuName = "FMOD/Radio/Radio Speaker", order = 1000_2)]
    public class RadioSpeaker : ScriptableObject {
        private static readonly Dictionary<SpeakerRole, string> _roleToDescription = new() {
            { SpeakerRole.Host, "a radio station host" },
            { SpeakerRole.Ads, "a radio ads speaker" },
            { SpeakerRole.InterviewGeneric, "an interview partner" },
            { SpeakerRole.InterviewMayor, "the mayor of the city" }
        };
        public static ReadOnlyDictionary<SpeakerRole, string> RoleToDescription => new(_roleToDescription);

        [field: Header("Personal infos")]
        [field: SerializeField] public string SpeakerName { get; private set; }
        [field: SerializeField] public string StaticMood { get; private set; } = FMODDefaults.Radio.Speaker.StaticMood.FUNNY_SATIRICAL;
        [field: SerializeField] public SpeakersDynamicMood DynamicMood { get; private set; } = default(SpeakersDynamicMood);
        [field: SerializeField, Multiline(5)] public string BackgroundInfo { get; private set; }

        [field: Header("Voice settings")]
        [field: SerializeField] public string VoiceName { get; private set; }
        [field: SerializeField] public SpeakerVoiceSettings VoiceSettings { get; private set; }

        [field: Space]
        [field: SerializeField] public string BackupPremadeVoiceName { get; private set; }
        [field: SerializeField] public SpeakerVoiceSettings BackupVoiceSettings { get; private set; }

#if UNITY_EDITOR
        private void Reset() {
            VoiceSettings = new(SpeakerVoiceSettings.DEFAULT_STABILITY, SpeakerVoiceSettings.DEFAULT_SIM_BOOST);
            BackupVoiceSettings = new(SpeakerVoiceSettings.DEFAULT_STABILITY, SpeakerVoiceSettings.DEFAULT_SIM_BOOST);
        }

        private void OnValidate() {
            DynamicMood.ValidateParameters();
        }
#endif
    }
}