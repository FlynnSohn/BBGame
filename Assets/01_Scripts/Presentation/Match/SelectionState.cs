using BBGame.Core;

namespace BBGame.Presentation.Match
{
    /// <summary>선택 규칙이 손패에 묻는 것 (테스트에서 가짜 손패로 바꿔 끼운다)</summary>
    public interface IHandRules
    {
        int HandCount { get; }
        bool IsPowerPitch(int handIndex);
        bool CanTarget(int handIndex, ZoneId zone);
        ZoneId FirstZone(int handIndex);
    }

    /// <summary>
    /// 카드·칸 선택 상태 — 프로토타입 v12 bindPlay와 같은 규칙 (match_m2_screen_plan "조작 규칙").
    /// · 카드를 누르면 고른다. 고른 칸으로 그 카드를 못 던지면 카드의 첫 코스로 바꾼다. 같은 카드를 다시 누르면 풀린다(코스 먼저로 고른 칸은 남는다)
    /// · 칸을 먼저 누르면 "코스 먼저" — 화면은 그 칸으로 던질 수 있는 카드만 밝힌다
    /// · 고른 카드가 못 가는 칸을 누르면 카드 선택이 풀리고 칸 우선이 된다
    /// · 전력투구 직구는 칸을 고르지 않는다 (엔진이 착지를 정한다)
    /// Unity를 쓰지 않는 순수 클래스 — EditMode 테스트로 확인한다
    /// </summary>
    public sealed class SelectionState
    {
        /// <summary>고른 손패 번호 (없으면 −1)</summary>
        public int Card { get; private set; } = -1;
        /// <summary>고른 칸 (논리 칸, 타자 기준)</summary>
        public ZoneId? Zone { get; private set; }
        /// <summary>칸을 먼저 골랐다 (코스 먼저 모드)</summary>
        public bool ZoneFirst { get; private set; }

        public bool HasCard => Card >= 0;

        public void ClickCard(int handIndex, IHandRules hand)
        {
            if (handIndex < 0 || handIndex >= hand.HandCount) return;
            if (Card == handIndex)
            {
                Card = -1;
                if (!ZoneFirst) Zone = null;
                return;
            }
            Card = handIndex;
            if (hand.IsPowerPitch(handIndex))
            {
                // 프로토타입: 전력투구의 코스는 "zone9" 하나 — 고른 칸과 맞지 않으니 카드 우선으로 바뀐다
                Zone = null;
                ZoneFirst = false;
                return;
            }
            if (!(Zone.HasValue && hand.CanTarget(handIndex, Zone.Value)))
            {
                Zone = hand.FirstZone(handIndex);
                ZoneFirst = false;
            }
        }

        public void ClickZone(ZoneId zone, IHandRules hand)
        {
            if (Card >= 0)
            {
                if (!hand.IsPowerPitch(Card) && hand.CanTarget(Card, zone)) { Zone = zone; ZoneFirst = true; }
                else { Card = -1; Zone = zone; ZoneFirst = true; }   // 카드 밖 칸 → 코스 먼저
                return;
            }
            if (Zone.HasValue && Zone.Value == zone) { Zone = null; ZoneFirst = false; }
            else { Zone = zone; ZoneFirst = true; }
        }

        /// <summary>던지기 명령 — 전력투구 직구는 칸 없이</summary>
        public ThrowCommand ToCommand(IHandRules hand)
            => new ThrowCommand(Card, Card >= 0 && hand.IsPowerPitch(Card) ? (ZoneId?)null : Zone);

        /// <summary>코스 먼저 모드에서 이 카드가 고른 칸으로 갈 수 있는가 (전력투구는 존 안이면 된다)</summary>
        public bool Fits(int handIndex, IHandRules hand)
        {
            if (!Zone.HasValue) return true;
            if (hand.IsPowerPitch(handIndex)) return !new BBGame.Core.Zone(Zone.Value).Outside;
            return hand.CanTarget(handIndex, Zone.Value);
        }

        public void Clear()
        {
            Card = -1;
            Zone = null;
            ZoneFirst = false;
        }

        /// <summary>손패 장수가 줄었을 때 범위 밖 선택을 정리한다</summary>
        public void Validate(IHandRules hand)
        {
            if (Card >= hand.HandCount) Clear();
        }
    }

    /// <summary>입력 잠금 — 리빌·안내 중에는 카드·칸·던지기 입력을 받지 않는다</summary>
    public sealed class InputGate
    {
        public bool IsLocked { get; private set; }
        public void Lock() => IsLocked = true;
        public void Unlock() => IsLocked = false;
    }
}
