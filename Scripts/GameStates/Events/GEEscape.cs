using UnityEngine;
using AdBlocker.World;

namespace AdBlocker.FMOD.GameStates.Events {
    public class GEEscape : GameStateEvent {
        [field: SerializeField] public DistrictType District { get; private set; }

        public GEEscape(DistrictType district) : base(GSEventType.Escape) {
            District = district;
        }

        public override string ConvertToText() {
            return $"The player escaped the police in district '{StringifyEnumType(District)}'";
        }

        protected override string GetDBValueParams() {
            return GetDBValueParam(nameof(District), District.ToString(), true);
        }
    }
}