namespace AdBlocker.FMOD.GameStates.Values {
    public class GVIntensity : GameStateValue<GVIntensity.Intensity> {
        public override string DBKey => null;
        public override string DBValue => null;

        public GVIntensity() : base(GSValueType.Intensity) { }

        public override string ConvertToText() => null;
        protected override void UpdateValue() { }

        public void SetIntensity(Intensity intensity, bool immediateFmodUpdate = true) {
            if (!_freezeValue) {
                Value = intensity;
                if (immediateFmodUpdate) FMODManagers.GameStateExtractor?.UpdateValues();
            }
        }

        public override GameStateValue<Intensity>[] GetOfflineStates() => null;

        public enum Intensity : byte {
            MenuLow,
            MenuHigh,
            OverworldLow,
            OverworldHigh,
            Instance
        }
    }
}