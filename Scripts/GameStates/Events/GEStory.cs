using UnityEngine;

namespace AdBlocker.FMOD.GameStates.Events {
    public class GEStory : GameStateEvent {
        [field: SerializeField, Multiline] public string Description { get; private set; }

        public GEStory(string description) : base(GSEventType.Story) {
            Description = description;
        }

        public override string ConvertToText() {
            return $"Important story event:\n{Description}";
        }

        protected override string GetDBValueParams() {
            return GetDBValueParam(nameof(Description), Description.ToString(), true);
        }
    }
}