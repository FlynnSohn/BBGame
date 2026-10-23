using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace BBGame.Presentation.Common
{
    /// <summary>
    /// 고정 문구 라벨 — 칸 제목·버튼 글자. 언어가 바뀌면 스스로 다시 그린다.
    /// LocalizeStringEvent 대신 쓰는 이유: 에디터 빌더가 문구만 넣으면 되고(이벤트 연결이 필요 없다), TMP 하나에만 쓴다
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private LocalizedString _text = new LocalizedString();

        private TMP_Text _label;

        private void Awake() => _label = GetComponent<TMP_Text>();

        // 구독하는 순간 현재 언어 문구를 불러와 한 번 부른다 (초기화가 끝나지 않았으면 끝난 뒤)
        private void OnEnable()
        {
            if (!_text.IsEmpty) _text.StringChanged += OnStringChanged;
        }

        private void OnDisable()
        {
            if (!_text.IsEmpty) _text.StringChanged -= OnStringChanged;
        }

        private void OnStringChanged(string value) => _label.text = value;

        /// <summary>에디터 빌더 전용 — 문구 참조를 넣는다</summary>
        internal void SetText(LocalizedString text) => _text = text;
    }
}
