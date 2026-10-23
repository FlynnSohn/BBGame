using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// [던진다] 버튼과 미리보기 한 줄 — 구위 × (1 + 가산) = 기본 제압치 × ? (§20-4: 읽기 배수는 ?로 둔다).
    /// 자리는 임시다 (D11, F5)
    /// </summary>
    public sealed class ThrowButtonView : MonoBehaviour
    {
        [SerializeField] internal Button _button;
        [SerializeField] internal TMP_Text _preview;
        [SerializeField] internal TMP_Text _cost;

        public event Action Clicked;

        private void Awake() => _button.onClick.AddListener(OnClick);
        private void OnDestroy() => _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            // 버튼이 선택된 채로 남으면 Enter(UI Submit)가 한 번 더 누른다 — 선택을 지운다
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Clicked?.Invoke();
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void Set(bool interactable, string preview, string cost)
        {
            _button.interactable = interactable;
            _preview.text = preview;
            _cost.text = cost;
        }
    }
}
