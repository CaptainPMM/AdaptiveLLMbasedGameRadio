using UnityEngine;
using AdBlocker.World;

namespace AdBlocker.FMOD.GameStates.Events {
    public class GECapture : GameStateEvent {
        [field: SerializeField] public DistrictType District { get; private set; }

        public GECapture(DistrictType district) : base(GSEventType.Capture) {
            District = district;
        }

        public override string ConvertToText() {
            return $"The player was captured by the police in district '{StringifyEnumType(District)}'";
        }

        protected override string GetDBValueParams() {
            return GetDBValueParam(nameof(District), District.ToString(), true);
        }
    }
}