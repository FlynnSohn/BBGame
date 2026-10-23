using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 끌고 있는 카드의 복사본 — 맨 위 캔버스에 그린다. 레이캐스트를 막지 않아야 아래 존 칸이 놓기(OnDrop)를 받는다
    /// </summary>
    public sealed class DragGhostView : MonoBehaviour
    {
        [SerializeField] internal RectTransform _root;
        [SerializeField] internal CanvasGroup _group;
        [SerializeField] internal TMP_Text _name;

        private RectTransform _parent;
        private Camera _camera;

        public bool IsShown => _root.gameObject.activeSelf;

        private void Awake()
        {
            _parent = (RectTransform)_root.parent;
            var canvas = GetComponentInParent<Canvas>();
            _camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            _group.blocksRaycasts = false;
            _root.gameObject.SetActive(false);
        }

        public void Show(string title, PointerEventData eventData)
        {
            _name.text = title;
            _root.gameObject.SetActive(true);
            Move(eventData);
        }

        public void Move(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, eventData.position, _camera, out var local))
                _root.anchoredPosition = local;
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
