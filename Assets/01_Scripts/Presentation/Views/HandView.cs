using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 손패 — 카드 8장을 미리 만들어 두고 켜고 끈다 (손패 최대 7 + 「넓은 손패」 1).
    /// 누르기·끌기는 이벤트로 알리고, 끄는 동안 고스트를 움직인다
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField] internal CardView[] _cards;
        [SerializeField] internal DragGhostView _ghost;

        public event Action<int> CardClicked;
        /// <summary>끌기 시작 (손패 번호) — 받는 쪽이 거절하면 고스트를 띄우지 않는다</summary>
        public event Func<int, bool> DragStarting;
        public event Action<int> DragEnded;

        public int Capacity => _cards.Length;
        public CardView Card(int index) => _cards[index];

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>앞에서부터 count장만 켠다 (나머지는 숨김)</summary>
        public void SetCount(int count)
        {
            for (int i = count; i < _cards.Length; i++) _cards[i].Hide();
        }

        /// <summary>끌던 고스트를 치운다 — 놓자마자 던져 손패가 숨으면 OnEndDrag가 오지 않으므로 Presenter가 부른다</summary>
        public void CancelDrag() => _ghost.Hide();

        internal void NotifyClick(int index) => CardClicked?.Invoke(index);

        internal void NotifyBeginDrag(CardView card, PointerEventData eventData)
        {
            if (DragStarting == null || !DragStarting(card.Index)) return;
            _ghost.Show(card.Title, eventData);
        }

        internal void NotifyDrag(PointerEventData eventData)
        {
            if (_ghost.IsShown) _ghost.Move(eventData);
        }

        internal void NotifyEndDrag(int index)
        {
            if (!_ghost.IsShown) return;
            _ghost.Hide();
            DragEnded?.Invoke(index);
        }
    }
}
