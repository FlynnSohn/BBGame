using BBGame.Core;
using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 전광판 — 라인스코어 1~9회 + R·H·E (Q1), 원정(상대) 위 · 홈(해신고) 아래 (Q2), B·S·O, 주자 다이아몬드 (Q3).
    /// 그리기만 한다. 값은 Presenter가 넘긴다
    /// </summary>
    public sealed class ScoreboardView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _awayName, _homeName;
        [Tooltip("1~9회 칸 (원정)")] [SerializeField] internal TMP_Text[] _awayInnings;
        [Tooltip("1~9회 칸 (홈)")] [SerializeField] internal TMP_Text[] _homeInnings;
        [SerializeField] internal TMP_Text _awayR, _awayH, _awayE, _homeR, _homeH, _homeE;
        [Tooltip("볼 3 · 스트라이크 2 · 아웃 2")]
        [SerializeField] internal Image[] _ballPips, _strikePips, _outPips;
        [Tooltip("0 = 1루(오른쪽) · 1 = 2루(위) · 2 = 3루(왼쪽) — 전광판 관례")]
        [SerializeField] internal Image[] _bases;

        public void SetTeams(string away, string home)
        {
            _awayName.text = away;
            _homeName.text = home;
        }

        /// <summary>
        /// 라인스코어. 상대(원정)는 진행 중인 이닝까지, 우리(홈)는 공격을 마친 이닝까지. 우리 팀 H는 지금 늘 0 (사용자 결정 2026-10-24).
        /// 진행 중인 이닝 칸은 강조색
        /// </summary>
        public void SetLine(IMatchReadOnly s, UiPalette p)
        {
            int now = s.Inning - 1;
            for (int i = 0; i < _awayInnings.Length; i++)
            {
                SetCell(_awayInnings[i], i < s.LineInnings && i <= now, i < s.LineInnings ? s.GetLineOpp(i) : 0);
                _awayInnings[i].color = i == now ? p.warnText : p.text;
            }
            // 우리(홈)는 회말 공격을 한 칸만 쓴다. 9회초 끝에 앞서 있으면 X (전광판 기호라 번역하지 않는다)
            for (int i = 0; i < _homeInnings.Length; i++)
            {
                if (i < s.LineInnings && s.HasLineUs(i) && s.IsLineUsX(i)) _homeInnings[i].SetText("X");
                else SetCell(_homeInnings[i], i < s.LineInnings && s.HasLineUs(i), i < s.LineInnings ? s.GetLineUs(i) : 0);
            }

            _awayR.SetText("{0}", s.ScoreOpp);
            _awayH.SetText("{0}", s.HitsOpp);
            _awayE.SetText("{0}", 0);   // 상대 실책 — 우리 공격 시뮬레이션에 실책이 들어오기 전까지 0
            _homeR.SetText("{0}", s.ScoreUs);
            _homeH.SetText("{0}", s.HitsUs);
            _homeE.SetText("{0}", s.ErrorsUs);
        }

        /// <summary>볼카운트·아웃·주자. 리빌 중에는 결과의 "던진 뒤" 값으로 그린다 (D2)</summary>
        public void SetCount(int balls, int strikes, int outs, BaseMask bases, UiPalette p)
        {
            Pips(_ballPips, balls, p.ball, p.pipOff);
            Pips(_strikePips, strikes, p.strike, p.pipOff);
            Pips(_outPips, outs, p.outPip, p.pipOff);
            _bases[0].color = bases.First ? p.baseOn : p.baseOff;
            _bases[1].color = bases.Second ? p.baseOn : p.baseOff;
            _bases[2].color = bases.Third ? p.baseOn : p.baseOff;
        }

        private static void SetCell(TMP_Text cell, bool shown, int value)
        {
            if (shown) cell.SetText("{0}", value);
            else cell.SetText(string.Empty);
        }

        private static void Pips(Image[] pips, int on, Color onColor, Color offColor)
        {
            for (int i = 0; i < pips.Length; i++) pips[i].color = i < on ? onColor : offColor;
        }
    }
}
