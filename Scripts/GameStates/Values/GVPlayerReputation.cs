using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AdBlocker.FMOD.GameStates.Values {
    public class GVPlayerReputation : GameStateValue<GVPlayerReputation.PlayerReputation> {
        private static readonly Dictionary<PlayerReputation, string> REP_TO_PLAYER_ALIAS = new() {
            { PlayerReputation.Unknown, "unknown person" },
            { PlayerReputation.Noticed, "a random name (make up a mysterious name)" },
            { PlayerReputation.Named, "The Adblocker" },
            { PlayerReputation.Famous, "The Adblocker" },
        };
        public static ReadOnlyDictionary<PlayerReputation, string> ReputationToPlayerAlias => new(REP_TO_PLAYER_ALIAS);

        public override string DBKey => "reputation";
        public override string DBValue => Value.ToString().ToLower();

        public GVPlayerReputation() : base(GSValueType.PlayerReputation) { }

        public override string ConvertToText() {
            return $"Player reputation: {StringifyEnumType(Value)}"; // maybe a switch case and a more detailed text description of the player rep is needed
        }

        protected override void UpdateValue() {
            switch (FMODManagers.GameStateExtractor.GetGameStateValueObj<GVPlayerProgression>().Value) {
                case <= 15:
                    Value = PlayerReputation.Unknown;
                    break;
                case <= 50:
                    Value = PlayerReputation.Noticed;
                    break;
                case <= 75:
                    Value = PlayerReputation.Named;
                    break;
                case > 75:
                    Value = PlayerReputation.Famous;
                    break;
            }
        }

        public override GameStateValue<PlayerReputation>[] GetOfflineStates() {
            return new GameStateValue<PlayerReputation>[] {
                new GVPlayerReputation().SetState(PlayerReputation.Unknown),
                new GVPlayerReputation().SetState(PlayerReputation.Noticed),
                new GVPlayerReputation().SetState(PlayerReputation.Named),
                new GVPlayerReputation().SetState(PlayerReputation.Famous)
            };
        }

        public enum PlayerReputation : byte {
            Unknown,
            Noticed,
            Named,
            Famous
        }
    }
}