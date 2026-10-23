using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>3택1 카드 한 장에 들어갈 문구</summary>
    public struct BonusChoiceTexts
    {
        public string Name;
        /// <summary>"Lv 1 → 2" 또는 "진화"</summary>
        public string Level;
        public string Effect;
        /// <summary>진화 조건 (진화일 때만)</summary>
        public string Note;
        public bool Evolution;
    }

    /// <summary>
    /// 이닝 보너스 3택1 (§13) — 카드 3장, 진화는 골드. 시간 제한 없음. 누르기 또는 단축키 1~3
    /// </summary>
    public sealed class BonusPickView : MonoBehaviour
    {
        [SerializeField] internal Button[] _choices;
        [SerializeField] internal Image[] _frames;
        [SerializeField] internal TMP_Text[] _names, _levels, _effects, _notes;

        private UniTaskCompletionSource<int> _waiting;
        private int _count;

        public bool IsShown => gameObject.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < _choices.Length; i++)
            {
                int index = i;   // 시작할 때 한 번만 — 버튼마다 번호를 묶는다
                _choices[i].onClick.AddListener(() => Choose(index));
            }
        }

        /// <summary>후보를 보여 주고 고를 때까지 기다린다. 고른 번호를 돌려준다</summary>
        public async UniTask<int> ShowAsync(BonusChoiceTexts[] choices, int count, Common.UiPalette p, CancellationToken ct)
        {
            _count = count;
            for (int i = 0; i < _choices.Length; i++)
            {
                bool on = i < count;
                _choices[i].gameObject.SetActive(on);
                if (!on) continue;
                var c = choices[i];
                _names[i].text = c.Name;
                _levels[i].text = c.Level;
                _effects[i].text = c.Effect;
                _notes[i].text = c.Note;
                _notes[i].gameObject.SetActive(!string.IsNullOrEmpty(c.Note));
                _frames[i].color = c.Evolution ? p.gold : p.panelBorder;
                _levels[i].color = c.Evolution ? p.goldBorder : p.accent;
            }
            gameObject.SetActive(true);
            _waiting = new UniTaskCompletionSource<int>();
            try { return await _waiting.Task.AttachExternalCancellation(ct); }
            finally { if (this != null) gameObject.SetActive(false); }
        }

        /// <summary>고르기 — 버튼과 단축키(1~3)가 부른다. 범위 밖이면 무시</summary>
        public void Choose(int index)
        {
            if (index < 0 || index >= _count) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            var w = _waiting;
            _waiting = null;
            w?.TrySetResult(index);
        }
    }
}
