using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// '오늘' 칩 — 판정 방식·날씨·컨디션·불펜·이닝 보너스. 미리 만든 칩을 켜고 끄고, 모자라면 첫 칩을 복제해 같은 간격으로 줄을 늘린다.
    /// 줄이 늘면 패널도 아래로 길어진다 (복제는 칩이 처음 모자랄 때 한 번 — 그 뒤로는 할당 없음)
    /// </summary>
    public sealed class TodayChipsView : MonoBehaviour
    {
        [SerializeField] internal Image[] _chips;
        [SerializeField] internal TMP_Text[] _labels;

        private readonly List<Image> _pool = new List<Image>(12);
        private readonly List<TMP_Text> _poolLabels = new List<TMP_Text>(12);
        private RectTransform _panel;
        private float _baseHeight, _rowStep;
        private Vector2 _colStep, _origin;
        private int _columns = 1;
        private int _used;

        private void Awake()
        {
            _panel = (RectTransform)transform;
            _baseHeight = _panel.sizeDelta.y;
            _pool.AddRange(_chips);
            _poolLabels.AddRange(_labels);
            for (int i = 0; i < _labels.Length; i++) FitText(_labels[i]);
            // 첫 줄의 칩 수로 열, 처음 줄이 바뀌는 칩으로 줄 간격을 읽는다
            _origin = Pos(0);
            for (int i = 1; i < _chips.Length; i++)
            {
                if (Mathf.Approximately(Pos(i).y, _origin.y)) { _columns = i + 1; _colStep = Pos(1) - _origin; continue; }
                _rowStep = _origin.y - Pos(i).y;
                break;
            }
            if (_rowStep <= 0) _rowStep = ((RectTransform)_chips[0].transform).sizeDelta.y + 4;
        }

        private Vector2 Pos(int i) => ((RectTransform)_chips[i].transform).anchoredPosition;

        public void Begin() => _used = 0;

        /// <summary>칩 하나를 켠다</summary>
        public void Add(string text, Color background, Color textColor)
        {
            if (_used >= _pool.Count) Grow();
            var chip = _pool[_used];
            chip.gameObject.SetActive(true);
            chip.color = background;
            _poolLabels[_used].text = text;
            _poolLabels[_used].color = textColor;
            _used++;
        }

        public void End()
        {
            for (int i = _used; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
            // 프리팹 칩 줄 수를 넘으면 패널을 늘린다
            int baseRows = (_chips.Length + _columns - 1) / _columns;
            int rows = (_used + _columns - 1) / _columns;
            var size = _panel.sizeDelta;
            size.y = _baseHeight + Mathf.Max(0, rows - baseRows) * _rowStep;
            _panel.sizeDelta = size;
        }

        private void Grow()
        {
            int i = _pool.Count;
            var clone = Instantiate(_chips[0], _chips[0].transform.parent);
            clone.name = "Chip" + i;
            var rt = (RectTransform)clone.transform;
            rt.anchoredPosition = _origin + _colStep * (i % _columns) - new Vector2(0, _rowStep * (i / _columns));
            _pool.Add(clone);
            var label = clone.GetComponentInChildren<TMP_Text>(true);
            FitText(label);
            _poolLabels.Add(label);
        }

        /// <summary>긴 칩 문구는 글자를 줄여 칸에 맞춘다 (지금 크기가 최대)</summary>
        private static void FitText(TMP_Text t)
        {
            if (t.enableAutoSizing) return;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = Mathf.Min(8f, t.fontSize);
            t.enableAutoSizing = true;
        }
    }
}
