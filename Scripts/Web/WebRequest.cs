using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AdBlocker.FMOD.Web {
    public static class WebRequest {
        public const int DEFAULT_TIMEOUT = 10;

        public async static Task<WebResult> Get(string url, KeyValuePair<string, string>[] getParams = null, KeyValuePair<string, string>[] headers = null, int timeout = DEFAULT_TIMEOUT) {
            // Add optional GET parameters to url
            if (getParams != null && getParams.Length > 0) {
                url += "?";
                foreach (var p in getParams) url += $"{p.Key}={p.Value}&";
                url = url.Remove(url.Length - 1); // remove last '&'
            }

            // Web request
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
                return await SendWebRequest(webRequest, headers, timeout);
            }
        }

        public async static Task<WebResult> Post(string url, string body, KeyValuePair<string, string>[] headers = null, int timeout = DEFAULT_TIMEOUT) {
            using (UnityWebRequest webRequest = UnityWebRequest.Put(url, body)) { // Post implementation is bad for JSON
                webRequest.method = UnityWebRequest.kHttpVerbPOST; // its also important to set the Content-Type header
                return await SendWebRequest(webRequest, headers, timeout);
            }
        }

        private async static Task<WebResult> SendWebRequest(UnityWebRequest webRequest, KeyValuePair<string, string>[] headers, int timeout) {
            // Add optional Headers
            if (headers != null && headers.Length > 0) {
                foreach (var h in headers) webRequest.SetRequestHeader(h.Key, h.Value);
            }

            // Send web request
            webRequest.timeout = timeout;
            var asyncRequestOperation = webRequest.SendWebRequest();

            // Wait for web request completion...
            await WaitForWebRequest(asyncRequestOperation);

            // Handle response
            if (string.IsNullOrWhiteSpace(webRequest.error) && webRequest.responseCode < 400) {
                // All good
                return new WebResult(webRequest, webRequest.downloadHandler.text, webRequest.downloadHandler.data);
            } else {
                // Error occured
                return new WebResult(webRequest, webRequest.downloadHandler.text, webRequest.downloadHandler.data, true, "WebRequest Error: " + webRequest.downloadHandler.text + ": " + webRequest.error);
            }
        }

        private async static Task WaitForWebRequest(UnityWebRequestAsyncOperation asyncRequestOperation) {
            while (!asyncRequestOperation.isDone) await Task.Yield();
        }
    }
}