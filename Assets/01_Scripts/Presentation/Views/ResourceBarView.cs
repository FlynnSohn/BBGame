using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>스태미나·집중력 공용 막대 — 현재/최대, 막대, 보조 줄</summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _value;
        [Tooltip("Image Type = Filled, Horizontal")]
        [SerializeField] internal Image _fill;
        [SerializeField] internal TMP_Text _sub;

        public void Set(int current, int max, string sub, Color fill)
        {
            _value.SetText("{0} / {1}", current, max);
            _fill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            _fill.color = fill;
            _sub.text = sub;
        }
    }
}
