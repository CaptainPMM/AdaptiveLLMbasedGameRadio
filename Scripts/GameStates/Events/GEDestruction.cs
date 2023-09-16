using UnityEngine;
using AdBlocker.Ads;

namespace AdBlocker.FMOD.GameStates.Events {
    public class GEDestruction : GameStateEvent {
        [field: SerializeField] public AdInfos AdInfos { get; private set; }

        public GEDestruction(AdInfos adInfos) : base(GSEventType.Destruction) {
            AdInfos = adInfos;
        }

        public override string ConvertToText() {
            return
$@"Ad destructed by the player:
* ad type: {StringifyEnumType(AdInfos.type)}
* importance: {StringifyEnumType(AdInfos.importance)}
* ad description: '{AdInfos.description}'
* district: {StringifyEnumType(AdInfos.district)}"
            ;
        }

        protected override string GetDBValueParams() {
            return
                GetDBValueParam(nameof(AdInfos.type), AdInfos.type.ToString()) +
                GetDBValueParam(nameof(AdInfos.importance), AdInfos.importance.ToString()) +
                GetDBValueParam(nameof(AdInfos.description), AdInfos.description?.ToString()) +
                GetDBValueParam(nameof(AdInfos.district), AdInfos.district.ToString(), true);
        }
    }
}