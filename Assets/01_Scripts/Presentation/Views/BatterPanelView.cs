using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>타자 패널 — 타순·이름, 유형 칩, 학년·포지션·타석, 시즌 기록, 성향·특징, 스카우팅, 제압 게이지</summary>
    public sealed class BatterPanelView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _header;
        [SerializeField] internal Image _kindChip;
        [SerializeField] internal TMP_Text _kind;
        [SerializeField] internal TMP_Text _info;
        [SerializeField] internal TMP_Text _season;
        [Tooltip("성향 줄 (1단계부터)")] [SerializeField] internal GameObject _personaRow;
        [SerializeField] internal TMP_Text _persona;
        [SerializeField] internal TMP_Text _feature;
        [Tooltip("스카우팅 리포트 (2단계)")] [SerializeField] internal GameObject _scoutRow;
        [SerializeField] internal GaugeView _gauge;

        public GaugeView Gauge => _gauge;

        /// <summary>단계 스위치로 줄을 켜고 끈다 (§6-3) — View가 단계를 직접 읽지 않는다</summary>
        public void SetStageRows(bool persona, bool scout)
        {
            _personaRow.SetActive(persona);
            _scoutRow.SetActive(scout);
        }

        public void SetBatter(string header, string kind, Color kindColor, string info, string season, string persona, string feature, bool featureKnown, UiPalette p)
        {
            _header.text = header;
            _kind.text = kind;
            _kindChip.color = kindColor;
            _info.text = info;
            _season.text = season;
            _persona.text = persona;
            _feature.text = feature;
            _feature.color = featureKnown ? p.text : p.textDim;
        }
    }
}
