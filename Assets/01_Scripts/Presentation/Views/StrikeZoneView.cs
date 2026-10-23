using System;
using BBGame.Core;
using TMPro;
using UnityEngine;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 투수 시점 17분할 존 (§5-2). 칸 17개는 프리팹에 고정 배치하고, 어느 칸이 몸쪽인지는 타자 손에 따라 Presenter가 정한다.
    /// 칸 입력은 이벤트로 알린다 — 화면 칸 번호로
    /// </summary>
    public sealed class StrikeZoneView : MonoBehaviour
    {
        [Tooltip("화면 칸 순서(ZoneId 순서)대로 17개")]
        [SerializeField] internal ZoneCellView[] _cells;
        [Tooltip("우타석 / 좌타석 표시")]
        [SerializeField] internal TMP_Text _boxLabelLeft, _boxLabelRight;

        /// <summary>칸을 눌렀다 (화면 칸)</summary>
        public event Action<ZoneId> CellClicked;
        /// <summary>카드를 칸에 놓았다 (화면 칸, 손패 번호)</summary>
        public event Action<ZoneId, int> CardDropped;

        public ZoneCellView Cell(ZoneId visual) => _cells[(int)visual];

        /// <summary>타석 표시 — 좌타자면 화면 왼쪽(1루 쪽)에</summary>
        public void SetBatterSide(Hand batterHand, string label)
        {
            bool right = batterHand == Hand.Right;
            _boxLabelRight.gameObject.SetActive(right);
            _boxLabelLeft.gameObject.SetActive(!right);
            (right ? _boxLabelRight : _boxLabelLeft).text = label;
        }

        internal void NotifyClick(ZoneId visual) => CellClicked?.Invoke(visual);
        internal void NotifyDrop(ZoneId visual, int handIndex) => CardDropped?.Invoke(visual, handIndex);
    }
}
