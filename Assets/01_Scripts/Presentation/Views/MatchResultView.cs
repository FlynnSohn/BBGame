using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 경기 결과 (간이, M3) — 승·패·무, 점수, 오늘 투수 기록 한 줄(§15), 강판했으면 구원 한 줄. 보상·덱 정리는 M8
    /// </summary>
    public sealed class MatchResultView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _title;
        [SerializeField] internal TMP_Text _score;
        [SerializeField] internal TMP_Text _pitcher;
        [SerializeField] internal TMP_Text _relief;
        [SerializeField] internal Button _button;
        [SerializeField] internal TMP_Text _buttonLabel;

        private UniTaskCompletionSource _waiting;

        public bool IsShown => gameObject.activeSelf;

        private void Awake() => _button.onClick.AddListener(Confirm);
        private void OnDestroy() => _button.onClick.RemoveListener(Confirm);

        public async UniTask ShowAsync(string title, Color titleColor, string score, string pitcher, string relief, string button, CancellationToken ct)
        {
            _title.text = title;
            _title.color = titleColor;
            _score.text = score;
            _pitcher.text = pitcher;
            _relief.text = relief;
            _relief.gameObject.SetActive(!string.IsNullOrEmpty(relief));
            _buttonLabel.text = button;
            gameObject.SetActive(true);
            _waiting = new UniTaskCompletionSource();
            try { await _waiting.Task.AttachExternalCancellation(ct); }
            finally { if (this != null) gameObject.SetActive(false); }
        }

        /// <summary>버튼과 단축키(Space·Enter)가 부른다</summary>
        public void Confirm()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            var w = _waiting;
            _waiting = null;
            w?.TrySetResult();
        }
    }
}
