using System.Text.RegularExpressions;

namespace AdBlocker.FMOD.GameStates {
    public abstract class GameState {
        private static readonly Regex _SPLIT_PASCAL_CASE_RGX = new("(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])");

        public abstract string DBKey { get; }
        public abstract string DBValue { get; }

        /// <summary>
        /// Converts the game state into a DB route description:
        /// "/{DBKey}${DBValue}"
        /// </summary>
        public string DBRoute => $"/{DBKey}${DBValue}";

        /// <summary>
        /// Converts the game state into a textual description.
        /// </summary>
        public abstract string ConvertToText();

        /// <summary>
        /// e.g. enum type: RadioStation -> Radio Station
        /// </summary>
        public static string StringifyEnumType(System.Enum type) {
            return _SPLIT_PASCAL_CASE_RGX.Replace(type.ToString(), " ");
        }
    }
}