using UnityEngine;
using AdBlocker.World;

namespace AdBlocker.FMOD.GameStates.Events {
    public class GEChase : GameStateEvent {
        [field: SerializeField] public CauseType Cause { get; private set; }
        [field: SerializeField] public DistrictType District { get; private set; }

        public GEChase(CauseType cause, DistrictType district) : base(GSEventType.Chase) {
            Cause = cause;
            District = district;
        }

        public override string ConvertToText() {
            return
$@"The player was chased by the police:
* cause: {StringifyEnumType(Cause)}
* district: {StringifyEnumType(District)}"
            ;
        }

        protected override string GetDBValueParams() {
            return
                GetDBValueParam(nameof(Cause), Cause.ToString()) +
                GetDBValueParam(nameof(District), District.ToString(), true);
        }

        public enum CauseType : byte {
            Unknown,
            Destruction,
            Camera
        }
    }
}