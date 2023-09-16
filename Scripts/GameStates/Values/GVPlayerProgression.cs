using UnityEngine;
using AdBlocker.Progression;

namespace AdBlocker.FMOD.GameStates.Values {
    public class GVPlayerProgression : GameStateValue<byte> {
        public override string DBKey => null;
        public override string DBValue => null;

        public GVPlayerProgression() : base(GSValueType.PlayerProgression) { }

        public override string ConvertToText() => null;

        protected override void UpdateValue() {
            Value = (byte)Mathf.Clamp(Mathf.RoundToInt(((float)Progress.getCurrentProgressPoints() / (float)Progress.getTotalProgressPoints()) * 100f), 0, 100);
        }

        public override GameStateValue<byte>[] GetOfflineStates() => null;
    }
}