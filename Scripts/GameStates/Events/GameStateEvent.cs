using UnityEngine;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.GameStates.Events {
    [System.Serializable]
    public abstract class GameStateEvent : GameState {
        public override string DBKey => "event";
        public override string DBValue => $"{Type.ToString().ToLower()}({GetDBValueParams()})";

        [field: SerializeField, ReadOnly] public GSEventType Type { get; private set; }

        protected GameStateEvent(GSEventType type) {
            Type = type;
        }

        /// <summary>
        /// Return the game state event parameters/fields as a string for the DBValue.
        /// Format: $"{nameof(Parameter).ToLower()}={Parameter.ToString().ToLower()}" (use ampersand as seperator for multiple parameters)
        /// Call the helper function GetDBValueParam() as a formatting shortcut.
        /// </summary>
        protected abstract string GetDBValueParams();

        /// <summary>
        /// Helps with the construction of the string for GetDBValueParams().
        /// </summary>
        /// <param name="key">parameter name</param>
        /// <param name="value">parameter value as string</param>
        /// <param name="last">false (default): ampersand is added to the end; true: no ampersand, last parameter</param>
        /// <returns>parameter string for the function GetDBValueParams()</returns>
        protected string GetDBValueParam(string key, string value, bool last = false) {
            string result = $"{key.ToLower()}={value?.ToLower().Replace('\n', '-')}";
            if (!last) result += "&";
            return result;
        }
    }
}