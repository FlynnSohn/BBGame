using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BBGame.EditorTools.Data
{
    // proto_v12_data.json 모양 그대로 (Docs/tools/proto_export.js 출력). 에디터 전용

    public sealed class ProtoData
    {
        public string source;
        public List<PitchTypeDto> pitchTypes;
        public List<CardDto> cards;
        public List<string> starterDeck;
        public string fixedCard, todayCard;
        public List<BatterTypeDto> batterTypes;
        public List<SchoolDto> schools;
        /// <summary>TuningConfig 그룹별 값 — 키는 C# 필드 이름과 같다</summary>
        public JObject tuning;
        /// <summary>StageFeatures 필드 값 — 키는 C# 필드 이름과 같다</summary>
        public List<JObject> stages;
        /// <summary>이닝 보너스·진화 표, 우리 타자·불펜 이름 (M3)</summary>
        public BonusTableDto bonusTable;
    }

    public sealed class BonusTableDto
    {
        public List<BonusRowDto> bonuses;
        public List<EvolutionRowDto> evolutions;
        public List<string> ourBatters, bullpen;
    }

    public sealed class BonusRowDto
    {
        public string id, need;
        public int maxLevel;
        public double perLevel;
    }

    public sealed class EvolutionRowDto
    {
        public string evolution, first, second, need;
        public int firstLevel, secondLevel;
    }

    public sealed class PitchTypeDto
    {
        public string type, family, like;
        public double baseVelo;
        public bool cardOnly;
        public double groundBonus, whiffMul, whiffSameMul, whiffOppMul, hitOppMul, jamOppMul, whiffLowMul, shakeAdd;
    }

    public sealed class CardDto
    {
        public string id, pitchType, grade, special;
        public List<string> zones;
        public bool isPowerPitch, isFixed, isTemporary;
        public int stuffAdjust, costAdjust, veloAdjust;
    }

    public sealed class BatterTypeDto
    {
        public string key, kind;
        public double baseTarget, adjust, power, chase;
        public bool canBunt, isCourse;
        public List<double> weights;
    }

    public sealed class SchoolDto
    {
        public string id;
        public bool isPractice;
        public List<BatterDto> lineup;
    }

    public sealed class BatterDto
    {
        public string seedKey, hand, type, firstPitch;
        public int number, grade;
        public double pull, center, oppo, fastballBias, breakingBias, twoStrikeContact, twoStrikeChase, rispFactor;
        public List<double> zoneHit, zoneChase;
        public SeasonDto season;
    }

    public sealed class SeasonDto
    {
        public int atBats, hits, homeRuns, rbi;
    }
}
