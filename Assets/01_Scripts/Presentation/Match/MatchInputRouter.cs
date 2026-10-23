using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BBGame.Presentation.Match
{
    /// <summary>
    /// PC 단축키 (Q15) — PlayerInput(Behavior: Invoke C# Events)의 Match 맵을 받아 이벤트로 넘긴다.
    /// 액션은 시작할 때 이름으로 한 번 찾아 두고, 입력마다 참조만 비교한다 (문자열 비교·할당 없음).
    /// 1~8 카드 고르기 · Space/Enter 던지기(리빌·안내 중에는 다음) · Esc 선택 취소
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class MatchInputRouter : MonoBehaviour
    {
        private const string k_Map = "Match";
        private const int k_CardKeys = 8;

        [SerializeField] internal PlayerInput _input;

        private readonly InputAction[] _cards = new InputAction[k_CardKeys];
        private InputAction _confirm, _cancel;

        /// <summary>카드 단축키 (0부터)</summary>
        public event Action<int> CardKey;
        public event Action Confirm;
        public event Action Cancel;

        private void Awake()
        {
            if (_input == null) _input = GetComponent<PlayerInput>();
            var map = _input.actions != null ? _input.actions.FindActionMap(k_Map) : null;
            if (map == null)
            {
                Debug.LogError("[Match] Input Actions에 Match 맵이 없다 — 메뉴 BBGame/Setup/7을 실행한다");
                return;
            }
            for (int i = 0; i < k_CardKeys; i++) _cards[i] = map.FindAction("Card" + (i + 1));
            _confirm = map.FindAction("Confirm");
            _cancel = map.FindAction("Cancel");
        }

        private void OnEnable() => _input.onActionTriggered += OnActionTriggered;
        private void OnDisable() => _input.onActionTriggered -= OnActionTriggered;

        private void OnActionTriggered(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            var a = ctx.action;
            if (a == _confirm) { Confirm?.Invoke(); return; }
            if (a == _cancel) { Cancel?.Invoke(); return; }
            for (int i = 0; i < k_CardKeys; i++)
            {
                if (a != _cards[i]) continue;
                CardKey?.Invoke(i);
                return;
            }
        }
    }
}
