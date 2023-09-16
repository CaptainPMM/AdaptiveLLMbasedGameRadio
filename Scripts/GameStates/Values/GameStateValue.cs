using UnityEngine;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.GameStates.Values {
    [System.Serializable]
    public abstract class GameStateValue : GameState {
        [Header("Debug")]
        [SerializeField] protected bool _freezeValue;

        [field: Header("Value")]
        [field: SerializeField, ReadOnly] public GSValueType Type { get; private set; }

        protected GameStateValue(GSValueType type) {
            Type = type;
        }

        /// <summary>
        /// Updates the game state value.
        /// </summary>
        public void Update() {
            if (_freezeValue) return;
            UpdateValue();
        }

        /// <summary>
        /// Update the game state value.
        /// </summary>
        protected abstract void UpdateValue();
    }

    public abstract class GameStateValue<T> : GameStateValue {
        [field: SerializeField] public T Value { get; protected set; }

        protected GameStateValue(GSValueType type) : base(type) { }

        public GameStateValue<T> SetState(T value) {
            Value = value;
            return this;
        }

        public abstract GameStateValue<T>[] GetOfflineStates();
    }
}