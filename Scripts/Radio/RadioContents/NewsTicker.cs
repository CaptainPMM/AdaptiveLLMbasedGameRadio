using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdBlocker.Utils;
using TMPro;

namespace AdBlocker.FMOD.Radio.RadioContents {
    public class NewsTicker : MonoBehaviour {
        public static NewsTicker Inst { get; private set; }

        [SerializeField] private TMP_Text _text;
        [SerializeField] private RectTransform _eot;
        [SerializeField, Min(0)] private int _tickerSpeed = 1;

        private Queue<string> _news = new();
        private Coroutine _tickerRoutine = null;

        private UnityEngine.Camera _cam;
        private Vector3 _startPos;

        private void Awake() {
            Inst = this;
        }

        private void Start() {
            _cam = UnityEngine.Camera.main;
            _startPos = _text.transform.position;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Push news to the news ticker.
        /// </summary>
        /// <param name="news">the news</param>
        /// <param name="speaker">optional: speaker name</param>
        public void Push(string news, string speaker = null) {
            if (string.IsNullOrWhiteSpace(news)) return;

            TextBuilder tb = new("+++   ");
            if (!string.IsNullOrWhiteSpace(speaker)) tb.Add($"{speaker.ToUpper()}: ");
            tb.Add(news);

            _news.Enqueue(tb.Text);
            if (_tickerRoutine == null) {
                gameObject.SetActive(true);
                _tickerRoutine = StartCoroutine(TickerRoutine());
            }
        }

        private IEnumerator TickerRoutine() {
            while (_news.Count > 0) {
                _text.transform.position = _startPos;
                string news = _news.Dequeue();
                _text.text = news;

                while (_eot.position.x > 0f) {
                    yield return null;
                    if (_eot.position.x > Screen.width) while (_news.Count > 0) _text.text += "   " + _news.Dequeue();
                    Vector3 pos = _text.transform.position;
                    pos.x -= _tickerSpeed * Time.deltaTime;
                    _text.transform.position = pos;
                }
            }

            _tickerRoutine = null;
            gameObject.SetActive(false);
        }
    }
}