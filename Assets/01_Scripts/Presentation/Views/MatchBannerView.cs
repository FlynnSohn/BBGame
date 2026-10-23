using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 경기 안내 창 — 반 이닝 종료 · 강판 · 경기 종료 (M2 임시. M3에서 3택1·우리 공격·승패로 바뀐다)
    /// </summary>
    public sealed class MatchBannerView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _title;
        [SerializeField] internal TMP_Text _body;
        [SerializeField] internal Button _button;
        [SerializeField] internal TMP_Text _buttonLabel;

        private UniTaskCompletionSource _waiting;

        public bool IsShown => gameObject.activeSelf;

        private void Awake() => _button.onClick.AddListener(Confirm);
        private void OnDestroy() => _button.onClick.RemoveListener(Confirm);

        /// <summary>안내를 띄우고 버튼(또는 단축키)을 누를 때까지 기다린 뒤 닫는다</summary>
        public async UniTask ShowAsync(string title, string body, string buttonLabel, CancellationToken ct)
        {
            _title.text = title;
            _body.text = body;
            _buttonLabel.text = buttonLabel;
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
