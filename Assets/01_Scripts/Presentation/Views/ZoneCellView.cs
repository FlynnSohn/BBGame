using BBGame.Core;
using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>존 칸 한 개의 표시 상태</summary>
    public enum ZoneCellState : byte
    {
        Idle,
        /// <summary>고른 카드로 던질 수 있는 칸</summary>
        Lit,
        /// <summary>고른 칸</summary>
        Selected,
        /// <summary>리빌 — 실제로 들어간 칸 (찌르기 결과 색)</summary>
        Actual,
        /// <summary>리빌 — 의도한 칸 (제구 미스)</summary>
        Intended
    }

    /// <summary>
    /// 스트라이크 존 칸 — 누르기·카드 놓기를 받는다. 칸 번호는 "화면 칸"(투수 시점, In = 화면 왼쪽)이다.
    /// 논리 칸으로 바꾸는 일은 Presenter가 타자 손에 따라 한다 (PitcherView)
    /// </summary>
    public sealed class ZoneCellView : MonoBehaviour, IPointerClickHandler, IDropHandler
    {
        [SerializeField] internal StrikeZoneView _owner;
        [Tooltip("화면 칸 — 열은 화면 기준 (In = 왼쪽)")]
        [SerializeField] internal ZoneId _visual;
        [SerializeField] internal Image _fill;
        [SerializeField] internal Image _frame;
        [SerializeField] internal TMP_Text _count;

        public ZoneId Visual => _visual;

        public void Set(ZoneCellState state, int count, Color actualColor, UiPalette p)
        {
            _fill.color = state switch
            {
                ZoneCellState.Lit => p.zoneLit,
                ZoneCellState.Selected => p.zoneSelected,
                ZoneCellState.Actual => actualColor,
                ZoneCellState.Intended => p.zoneIntended,
                _ => new Zone(_visual).Outside ? p.zoneIdle : p.zoneInsideIdle,
            };
            if (count > 0) _count.SetText("{0}", count);
            else _count.SetText(string.Empty);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging) return;
            _owner.NotifyClick(_visual);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var card = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CardView>() : null;
            if (card != null) _owner.NotifyDrop(_visual, card.Index);
        }
    }
}
