using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using FMOD;
using FMOD.Studio;
using AdBlocker.FMOD.Radio.RadioContents;
using AdBlocker.FMOD.Radio.RadioStations;

namespace AdBlocker.FMOD.Radio {
    public partial class RadioManager : MonoBehaviour {
        private const string _NEXT_STAGE = "___NEXTSTAGE___";

        private const string _PROG_SOUND_TRIGGER_NAME_RLCC = "Trigger: Radio Loop Content Creation";

        private const string _PROG_SOUND_NAME_NEWS_SPEAKER = "News Speaker";
        private const string _PROG_SOUND_NAME_AD1_SPEAKER = "Ad 1 Speaker";
        private const string _PROG_SOUND_NAME_AD2_SPEAKER = "Ad 2 Speaker";

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static RESULT RadioContentCallbackHandler(EVENT_CALLBACK_TYPE cbType, IntPtr cbEvent, IntPtr cbParameters) {
            switch (cbType) {
                case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                    HandleProgSoundCreate(cbParameters);
                    break;
                case EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                    HandleProgSoundDestroy(cbParameters);
                    break;
            }

            return RESULT.OK;
        }

        private static void GetProgSound(IntPtr cbParameters, out PROGRAMMER_SOUND_PROPERTIES progSound) {
            progSound = Marshal.PtrToStructure<PROGRAMMER_SOUND_PROPERTIES>(cbParameters);
        }

        private static void HandleProgSoundCreate(IntPtr cbParameters) {
            byte[] mp3Data;

            GetProgSound(cbParameters, out var progSound);
            switch ((string)progSound.name) {
                case _PROG_SOUND_TRIGGER_NAME_RLCC:
                    _mainThreadActions.Enqueue(() => CreateRadioLoopContent());
                    return;
                case _PROG_SOUND_NAME_NEWS_SPEAKER:
                    // (TODO one could use another programmer instrument for the background music and play news content dependent music)
                    RadioContent newsContent = FMODManagers.RadioManager._radioLoopContents.Find(rc => rc.Type == RadioContentType.News);
                    if (newsContent == null || newsContent.HasException(out _)) goto case _NEXT_STAGE;
                    else {
                        if (newsContent.ConcatAudioContentSections.Length > 0) mp3Data = newsContent.ConcatAudioContentSections;
                        else {
                            _mainThreadActions.Enqueue(() => {
                                for (int i = 0; i < newsContent.TextContentSections.Count; i++) {
                                    NewsTicker.Inst.Push(newsContent.TextContentSections[i], newsContent.RadioSpeakers[i].SpeakerName);
                                }
                            });
                            goto case _NEXT_STAGE;
                        }
                    }
                    break;
                case _PROG_SOUND_NAME_AD1_SPEAKER:
                    RadioContent ad1Content = FMODManagers.RadioManager._radioLoopContents.Find(rc => rc.Type == RadioContentType.Ads);
                    if (ad1Content == null || ad1Content.HasException(out _)) goto case _NEXT_STAGE;
                    else {
                        if (ad1Content.ConcatAudioContentSections.Length > 0) mp3Data = ad1Content.AudioContentSections[0];
                        else {
                            _mainThreadActions.Enqueue(() => NewsTicker.Inst.Push(ad1Content.TextContentSections[0], "Ad"));
                            goto case _NEXT_STAGE;
                        }
                    }
                    break;
                case _PROG_SOUND_NAME_AD2_SPEAKER:
                    RadioContent ad2Content = FMODManagers.RadioManager._radioLoopContents.Find(rc => rc.Type == RadioContentType.Ads);
                    if (ad2Content == null || ad2Content.HasException(out _)) goto case _NEXT_STAGE;
                    else {
                        if (ad2Content.ConcatAudioContentSections.Length > 0) mp3Data = ad2Content.AudioContentSections[1];
                        else {
                            _mainThreadActions.Enqueue(() => NewsTicker.Inst.Push(ad2Content.TextContentSections[1], "Ad"));
                            goto case _NEXT_STAGE;
                        }
                    }
                    break;
                case _NEXT_STAGE:
                    FMODManagers.RadioManager.NextFMODStage(); // skip current stage
                    return;
                default:
                    UnityEngine.Debug.LogWarning($"RadioManager: programmer sound name <{(string)progSound.name}> unknown");
                    return;
            }

            var soundInfo = new CREATESOUNDEXINFO() {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = (uint)mp3Data.Length,
                suggestedsoundtype = SOUND_TYPE.MPEG
            };

            FMODUnity.RuntimeManager.CoreSystem.createSound(mp3Data, MODE.OPENMEMORY_POINT | MODE.CREATECOMPRESSEDSAMPLE | MODE.LOOP_NORMAL | MODE.NONBLOCKING, ref soundInfo, out var sound);

            progSound.sound = sound.handle;
            progSound.subsoundIndex = -1;
            Marshal.StructureToPtr(progSound, cbParameters, false);
        }

        private static void HandleProgSoundDestroy(IntPtr cbParameters) {
            GetProgSound(cbParameters, out var progSound);
            Sound sound = new Sound();
            sound.handle = progSound.sound;
            sound.release();
        }

        private static async void CreateRadioLoopContent() {
            RadioManager rm = FMODManagers.RadioManager;

            if (rm._waitingForRadioContent) return;
            rm._waitingForRadioContent = true;

            rm._radioLoopContents.Clear();

            RadioStation station = rm.GetActiveRadioStationData();

            RadioContent[] contentResults = await Task.WhenAll(FMODManagers.RadioContentCreator.CreateContent(station, RadioContentType.News),
                                                               FMODManagers.RadioContentCreator.CreateContent(station, RadioContentType.Ads));

            rm._radioLoopContents.AddRange(contentResults);

            rm._waitingForRadioContent = false;
        }
    }
}