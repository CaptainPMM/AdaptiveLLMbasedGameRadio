using UnityEngine;
using FMODUnity;
using AdBlocker.Player;

namespace AdBlocker.FMOD.Utils {
    [RequireComponent(typeof(StudioListener))]
    public class ListenerAttenuationToPlayer : MonoBehaviour {
        [SerializeField, Tooltip("Leave empty to use player root gameobject")]
        private string _targetGameObjectName = "";

        private void Awake() {
            StudioListener listener = GetComponent<StudioListener>();
            if (string.IsNullOrWhiteSpace(_targetGameObjectName))
                listener.AttenuationObject = FindObjectOfType<PlayerController>()?.gameObject;
            else
                listener.AttenuationObject = GameObject.Find(_targetGameObjectName);

            if (listener.AttenuationObject == null) Debug.LogWarning("StudioListener: could not set attenuation object to player");
        }
    }
}