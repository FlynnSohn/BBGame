using UnityEngine;

namespace BBGame.Presentation.Common
{
    /// <summary>
    /// 노치·홈 표시줄을 피하도록 이 RectTransform을 Screen.safeArea에 맞춘다.
    /// 캔버스 바로 아래 "SafeArea" 오브젝트에 붙이고, 화면 요소는 그 아래에 둔다.
    /// 배경처럼 화면 끝까지 덮어야 하는 것은 SafeArea 밖에 둔다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;

        // 마지막으로 적용한 값 — 바뀌었을 때만 다시 계산한다
        private Rect _lastSafe;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        // 회전·해상도 변화를 감지한다. 값 비교뿐이라 매 프레임 할당이 없다
        private void Update()
        {
            if (Screen.safeArea != _lastSafe || Screen.width != _lastWidth || Screen.height != _lastHeight)
                Apply();
        }

        /// <summary>현재 안전 영역을 앵커 비율로 바꿔 적용한다</summary>
        private void Apply()
        {
            _lastSafe = Screen.safeArea;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            // 창이 최소화되는 등 크기가 0이면 계산하지 않는다 (0으로 나누기 방지)
            if (_lastWidth <= 0 || _lastHeight <= 0)
                return;

            _rect.anchorMin = new Vector2(_lastSafe.xMin / _lastWidth, _lastSafe.yMin / _lastHeight);
            _rect.anchorMax = new Vector2(_lastSafe.xMax / _lastWidth, _lastSafe.yMax / _lastHeight);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
