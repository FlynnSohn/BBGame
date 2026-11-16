using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Common
{
    /// <summary>버튼 하나에 씬 이동 하나를 붙인다 (타이틀의 시작·종료, 경기 화면의 [타이틀로])</summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class SceneButton : MonoBehaviour
    {
        public enum Action { StartMatch, ToTitle, Quit }

        [SerializeField] internal Action _action;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _button.interactable = false;   // 두 번 눌러 씬을 두 번 불러오지 않게
            switch (_action)
            {
                case Action.StartMatch: SceneFlow.StartMatch(); break;
                case Action.ToTitle: SceneFlow.ToTitle(); break;
                case Action.Quit: SceneFlow.Quit(); _button.interactable = true; break;
            }
        }
    }
}
