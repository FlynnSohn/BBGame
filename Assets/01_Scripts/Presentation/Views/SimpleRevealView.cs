using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>간이 리빌에 들어갈 문구 — Presenter가 채운다 (순서는 화면 배치가 지킨다)</summary>
    public struct RevealTexts
    {
        public string Verdict;
        public Color VerdictColor;
        public string IntentName;
        public string StabLine;
        public Color StabColor;
        public string SwitchNote;
        public string GainLine;
        public string GaugeLine;
        public string ResultLine;
        public string Tag;
        public Color TagColor;
        public string Notes;
        public string NextLabel;
    }

    /// <summary>
    /// 간이 리빌 (M2) — 손패 자리를 대신하는 정적 패널. 위에서 아래로 §20-2 순서:
    /// 1 읽기 판정 큰 글씨 → 2 타자의 노림 → 3 제압치·게이지 → 4 결과 줄 → 5 노트 → [다음].
    /// 읽기 판정이 결과보다 항상 위다. 연출(카운트업·발광)은 M4
    /// </summary>
    public sealed class SimpleRevealView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _verdict;
        [SerializeField] internal TMP_Text _intentName;
        [SerializeField] internal TMP_Text _stabLine;
        [SerializeField] internal TMP_Text _switchNote;
        [SerializeField] internal TMP_Text _gainLine;
        [SerializeField] internal TMP_Text _gaugeLine;
        [SerializeField] internal TMP_Text _resultLine;
        [SerializeField] internal TMP_Text _tag;
        [SerializeField] internal TMP_Text _notes;
        [SerializeField] internal Button _next;
        [SerializeField] internal TMP_Text _nextLabel;

        private UniTaskCompletionSource _waiting;

        public bool IsShown => gameObject.activeSelf;

        private void Awake() => _next.onClick.AddListener(Next);
        private void OnDestroy() => _next.onClick.RemoveListener(Next);

        public void Show(in RevealTexts t)
        {
            _verdict.text = t.Verdict;
            _verdict.color = t.VerdictColor;
            _intentName.text = t.IntentName;
            _stabLine.text = t.StabLine;
            _stabLine.color = t.StabColor;
            _switchNote.text = t.SwitchNote;
            _switchNote.gameObject.SetActive(!string.IsNullOrEmpty(t.SwitchNote));
            _gainLine.text = t.GainLine;
            _gaugeLine.text = t.GaugeLine;
            _resultLine.text = t.ResultLine;
            _tag.text = t.Tag;
            _tag.color = t.TagColor;
            _notes.text = t.Notes;
            _nextLabel.text = t.NextLabel;
            gameObject.SetActive(true);
        }

        /// <summary>[다음]을 누를 때까지 기다린다 (버튼 또는 단축키 → Next)</summary>
        public UniTask WaitNextAsync(CancellationToken ct)
        {
            _waiting = new UniTaskCompletionSource();
            return _waiting.Task.AttachExternalCancellation(ct);
        }

        /// <summary>[다음] — 버튼과 단축키(Space·Enter)가 부른다</summary>
        public void Next()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            var w = _waiting;
            _waiting = null;
            w?.TrySetResult();
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
