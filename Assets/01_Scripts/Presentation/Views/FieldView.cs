using BBGame.Core;
using UnityEngine;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 필드 그림 — 배경 + 타자 그림 (D16 a: 모두 uGUI). 좌타자면 그림을 1루 쪽(화면 왼쪽) 자리로 옮기고 좌우를 뒤집기만 한다 (Q10).
    /// 자리는 배경 안의 비율 앵커로 둔다 — 배경이 화면비에 맞춰 커져도 함께 따라간다. 그림·자리는 나중에 바꾼다
    /// </summary>
    public sealed class FieldView : MonoBehaviour
    {
        [SerializeField] internal RectTransform _batter;
        [Tooltip("우타석 자리 — 배경 비율 앵커 (min.x, min.y, max.x, max.y)")]
        [SerializeField] internal Vector4 _rightBox;
        [Tooltip("좌타석 자리 — 배경 비율 앵커")]
        [SerializeField] internal Vector4 _leftBox;

        public void SetBatterHand(Hand hand)
        {
            if (_batter == null) return;
            var box = hand == Hand.Right ? _rightBox : _leftBox;
            _batter.anchorMin = new Vector2(box.x, box.y);
            _batter.anchorMax = new Vector2(box.z, box.w);
            _batter.offsetMin = Vector2.zero;
            _batter.offsetMax = Vector2.zero;
            _batter.localScale = new Vector3(hand == Hand.Right ? 1f : -1f, 1f, 1f);
        }
    }
}
