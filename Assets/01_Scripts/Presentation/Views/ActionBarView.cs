using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>하단 버튼 줄 — 타임·고의사구·사인교환. 아직 기능이 없어 1차 빌드에서는 모두 숨긴다 (build1_plan, M5·M6에서 동작)</summary>
    public sealed class ActionBarView : MonoBehaviour
    {
        [SerializeField] internal Button _time, _intentionalWalk, _sign;

        public void SetStage(bool signVisible)
        {
            // 눌러도 아무 일 없는 버튼은 보이지 않게 (build1_plan) — 기능이 생기면 단계별로 다시 켠다
            _time.gameObject.SetActive(false);
            _intentionalWalk.gameObject.SetActive(false);
            _sign.gameObject.SetActive(false);
        }
    }
}
