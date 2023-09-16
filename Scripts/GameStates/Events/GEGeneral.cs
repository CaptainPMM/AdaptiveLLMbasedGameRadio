using UnityEngine;

namespace AdBlocker.FMOD.GameStates.Events {
    public class GEGeneral : GameStateEvent {
        [field: SerializeField, Multiline] public string Description { get; private set; }

        public GEGeneral(string description) : base(GSEventType.General) {
            Description = description;
        }

        public override string ConvertToText() {
            return $"General event:\n{Description}";
        }

        protected override string GetDBValueParams() {
            return GetDBValueParam(nameof(Description), Description.ToString(), true);
        }
    }
}