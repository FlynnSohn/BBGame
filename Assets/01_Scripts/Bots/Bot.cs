using BBGame.Core;

namespace BBGame.Bots
{
    /// <summary>
    /// 수치 검토 봇 (사양 §19-3). 매 공 Pitching 단계에서 던질 투구 하나를 고른다.
    /// 에디터 전용 어셈블리라 게임 빌드에는 들어가지 않는다 (match_m9_bots_plan B1)
    /// </summary>
    public abstract class Bot
    {
        /// <summary>집계 표에 쓰는 이름 (개발용 — 게임 화면 문구가 아니다)</summary>
        public abstract string Name { get; }

        /// <summary>이번 공 — 손패 번호와 칸 (전력투구는 칸 null). 0단계는 선언이 없다</summary>
        public abstract ThrowCommand Choose(MatchEngine engine);
    }

    /// <summary>무작위 — 손패 × 칸 선택지 중 하나 (프로토타입 BOTS.random). 경기 난수를 쓴다</summary>
    public sealed class RandomBot : Bot
    {
        private readonly BotChooser _chooser;
        public RandomBot(GameDatabase db) { _chooser = new BotChooser(db); }
        public override string Name => "무작위";
        public override ThrowCommand Choose(MatchEngine e) => _chooser.Random(e);
    }

    /// <summary>
    /// 뒤집지 않음 — 보이는 상황만으로 1위 노림을 추정해 고른다 (프로토타입 BOTS.blind).
    /// 0단계는 단서가 없어 뒤집기·선언 봇과 같은 공을 던진다
    /// </summary>
    public sealed class BlindBot : Bot
    {
        private readonly BotChooser _chooser;
        private readonly TuningConfig _t;
        public BlindBot(GameDatabase db) { _chooser = new BotChooser(db); _t = db.Tuning; }
        public override string Name => "뒤집지 않음";
        public override ThrowCommand Choose(MatchEngine e) => _chooser.Best(e, BotView.TopIntent(e, _t));
    }

    /// <summary>정답 봇 — 이번 공 실제 노림을 안다 (상한선, 프로토타입 BOTS.oracle)</summary>
    public sealed class OracleBot : Bot
    {
        private readonly BotChooser _chooser;
        private readonly bool _zoneMap;
        public OracleBot(GameDatabase db)
        {
            _chooser = new BotChooser(db);
            // 프로토타입: botChoose(G, tgt, cfg.scout ? 진짜 핫/콜드 존 : null) — 존 효과(scZone)가 꺼져 있으면 쓰지 않는다
            _zoneMap = db.Tuning.scout.scout && db.Tuning.scout.zone;
        }
        public override string Name => "정답 봇";
        public override ThrowCommand Choose(MatchEngine e) => _chooser.Best(e, e.BotActualIntent, _zoneMap);
    }
}
