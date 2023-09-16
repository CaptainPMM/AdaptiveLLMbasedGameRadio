using UnityEngine.Networking;

namespace AdBlocker.FMOD.Web {
    [System.Serializable]
    public struct WebResult {
        public UnityWebRequest request;
        public string result;
        public byte[] data;
        public bool error;
        public string errorMsg;

        public WebResult(UnityWebRequest request, string result, byte[] data, bool error = false, string errorMsg = "") {
            this.request = request;
            this.result = result;
            this.data = data;
            this.error = error;
            this.errorMsg = errorMsg;
        }
    }
}