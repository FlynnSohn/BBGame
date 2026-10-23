using BBGame.Core;
using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 손패 카드 한 장 — 등급 배지 · 구종 태그 · 이름 · 코스 · 특수 효과 · "구위 n · 스태 n" (§20-4: 예상 구속은 넣지 않는다).
    /// 고르면 안쪽 내용(_content)만 들어 올린다 — 루트 위치는 LayoutGroup이 정하므로 건드리지 않는다.
    /// 끌기는 원본을 제자리에 두고 HandView가 고스트를 그린다 (D11)
    /// </summary>
    public sealed class CardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] internal HandView _owner;
        [SerializeField] internal int _index;
        [SerializeField] internal RectTransform _content;
        [SerializeField] internal CanvasGroup _group;
        [SerializeField] internal Image _background, _frame;
        [SerializeField] internal Image _gradeBadge;
        [SerializeField] internal TMP_Text _grade;
        [SerializeField] internal TMP_Text _type;
        [SerializeField] internal TMP_Text _name;
        [SerializeField] internal TMP_Text _course;
        [SerializeField] internal TMP_Text _special;
        [SerializeField] internal TMP_Text _stats;
        [Tooltip("골랐을 때 내용이 올라가는 높이")]
        [SerializeField] internal float _liftHeight = 18f;

        private static readonly string[] s_gradeLetters = { "N", "R", "S" };

        public int Index => _index;
        /// <summary>고스트에 쓸 이름</summary>
        public string Title => _name.text;

        public void Show(Grade grade, string type, string name, string course, string special, string stats, UiPalette p)
        {
            gameObject.SetActive(true);
            var gradeColor = grade == Grade.S ? p.gradeS : grade == Grade.R ? p.gradeR : p.gradeN;
            _gradeBadge.color = gradeColor;
            _grade.text = s_gradeLetters[(int)grade];   // 등급 기호 — 문장이 아니라 코드라 번역하지 않는다
            _type.text = type;
            _type.color = gradeColor;
            _name.text = name;
            _course.text = course;
            _special.text = special;
            _special.gameObject.SetActive(!string.IsNullOrEmpty(special));
            _stats.text = stats;
            _background.color = p.card;
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetState(bool selected, bool dimmed, UiPalette p)
        {
            _content.anchoredPosition = new Vector2(0f, selected ? _liftHeight : 0f);
            _frame.color = selected ? p.cardSelectedFrame : p.panelBorder;
            _group.alpha = dimmed ? p.cardDimAlpha : 1f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging) return;
            _owner.NotifyClick(_index);
        }

        public void OnBeginDrag(PointerEventData eventData) => _owner.NotifyBeginDrag(this, eventData);
        public void OnDrag(PointerEventData eventData) => _owner.NotifyDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => _owner.NotifyEndDrag(_index);
    }
}
