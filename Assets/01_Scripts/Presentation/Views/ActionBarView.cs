using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>하단 버튼 줄 — 타임·고의사구·사인교환. M2에서는 자리만 (M5·M6에서 동작)</summary>
    public sealed class ActionBarView : MonoBehaviour
    {
        [SerializeField] internal Button _time, _intentionalWalk, _sign;

        public void SetStage(bool signVisible)
        {
            _time.interactable = false;
            _intentionalWalk.interactable = false;
            _sign.interactable = false;
            _sign.gameObject.SetActive(signVisible);
        }
    }
}
