using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using AdBlocker.FMOD.LLMCommands.PromptConfigs;
using AdBlocker.FMOD.Radio.RadioStations;
using AdBlocker.FMOD.Radio.RadioStations.Speakers;
using AdBlocker.FMOD.Radio.RadioContents;
using AdBlocker.FMOD.GameStates.Values;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.LLMCommands {
    [System.Serializable]
    public class LLMCommand {
        public const string OUTPUT_PARAMS_SEPARATOR = "$$$";

        [field: Header("Types")]
        [field: SerializeField] public RadioContentType ContentType { get; private set; }
        [field: SerializeField] public LLMCommandType CmdType { get; private set; }

        [Header("Game Context")]
        [SerializeField] private List<GSValueType> _ctxGameStateValueTypes = new();
        public ReadOnlyCollection<GSValueType> CtxGameStateValueTypes => new(_ctxGameStateValueTypes);
        [field: SerializeField] public bool CtxOnlyRecentEvents { get; private set; } = true;
        [field: SerializeField, Min(-1)] public int CtxNumEvents { get; private set; } = FMODDefaults.LLMCommand.GameContext.GameStateEvents.FEW;

        [field: Header("Command Context")]
        [field: SerializeField] public bool UseCommandCtx { get; private set; } = false;

        [field: Header("Roles")]
        [SerializeField] private List<SpeakerRole> _roles = new();
        public ReadOnlyCollection<SpeakerRole> Roles => new(_roles);

        [field: Header("Prompt")]
        [field: SerializeField] public LLMObjective Objective { get; private set; } = default(LLMObjective);
        [field: SerializeField] public LLMFormat Format { get; private set; } = default(LLMFormat);
        [field: SerializeField] public LLMThingsToAvoid ThingsToAvoid { get; private set; } = default(LLMThingsToAvoid);

        [Header("Output")]
        // Output params: <key/what is the value? (e.g. 'gender'), value instruction for the LLM (e.g. 'male or female')>
        [SerializeField] private List<KVPair<string, string>> _outputParams = new();
        public ReadOnlyCollection<KVPair<string, string>> OutputParams => _outputParams.AsReadOnly();

        public LLMPrompt GetPrompt(RadioStation station, IEnumerable<RadioSpeaker> speakers, IList<SpeakerRole> speakerRoles, string commandCtx = null) {
            // SYSTEM
            TextBuilder system = new();
            if (!string.IsNullOrWhiteSpace(FMODManagers.RadioContentCreator.LLMGeneralCtx))
                system.AddLine($"General context:\n{FMODManagers.RadioContentCreator.LLMGeneralCtx}");
            if (CtxGameStateValueTypes.Count > 0 || CtxNumEvents != 0)
                system.AddLine($"Game context:\n{FMODManagers.GameStateExtractor.GetGameStateText(CtxGameStateValueTypes, CtxNumEvents, CtxOnlyRecentEvents)}");
            if (UseCommandCtx && !string.IsNullOrWhiteSpace(commandCtx))
                system.AddLine($"Command context:\n{commandCtx}");
            if (station)
                system.AddLine($"Radio station context:\n* name: {station.Name}\n* info: {station.StationCtx}");
            if (speakers != null) {
                system.AddLine($"Role(s):");
                int i = 0;
                foreach (RadioSpeaker speaker in speakers) {
                    system.AddLine($"{i + 1}. {RadioSpeaker.RoleToDescription[speakerRoles[i++]]}");
                    if (!string.IsNullOrWhiteSpace(speaker.SpeakerName)) system.Add($" called {speaker.SpeakerName}");
                    if (!string.IsNullOrWhiteSpace(speaker.BackgroundInfo)) system.Add($". {speaker.BackgroundInfo}");
                }

                system.AddLine($"Role Mood(s):");
                i = 0;
                foreach (RadioSpeaker speaker in speakers) {
                    string mood = speaker.DynamicMood.Get(speaker.StaticMood);
                    if (!string.IsNullOrWhiteSpace(speaker.SpeakerName)) system.AddLine($"{i + 1}. {speaker.SpeakerName} is {mood}");
                    else system.AddLine($"{i + 1}. role is {mood}");
                    i++;
                }
            }

            // COMMAND
            TextBuilder command = new TextBuilder()
                .AddLine($"Objective:\n{Objective.Get()}")
                .AddLine($"Format:\n{Format.Get(speakerRoles.Count)}")
                .AddLine($"Things to avoid:\n{ThingsToAvoid.Get()}");

            // OUTPUT PARAMS
            TextBuilder outputParams = new();
            if (OutputParams.Count > 0) {
                outputParams.Add($"At the very end of the output (after all speaker paragraphs) append exactly '{OUTPUT_PARAMS_SEPARATOR}");
                foreach (var kv in OutputParams) outputParams.Add($"{kv.key}: {kv.value},");
                outputParams.RemoveLast(); // remove last ','
                outputParams.Add("'");
            }

            return new LLMPrompt(ContentType, CmdType, system.Text, command.Text, outputParams.Text);
        }
    }
}