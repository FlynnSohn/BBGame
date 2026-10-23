using System;
using BBGame.Core;
using UnityEngine;
using UnityEngine.Localization;

namespace BBGame.Presentation.Data
{
    /// <summary>리빌 큰 글씨 — 읽기 판정 (§8-4, §20-5). 0단계는 찌르기 결과를 크게 쓴다</summary>
    public enum ReadVerdict : byte { Read, ReadNotStabbed, ReadButBad, Missed, MissedButStabbed, UndeclaredStabbed, UndeclaredThrown }

    /// <summary>게이지 상태 문구 (§20-3)</summary>
    public enum GaugeStatus : byte { Below, Dominate, Overwhelm }

    /// <summary>타자 패널 "몇 번째 타석" (다섯 번째까지 낱말, 그 뒤는 숫자)</summary>
    public enum PlateOrdinal : byte { First, Second, Third, Fourth, Fifth }

    /// <summary>스태미나 구간 0~3 문구 (§11-2)</summary>
    public enum StaminaLevel : byte { Fresh, Slight, Tired, Spent }

    /// <summary>문구 가져오기 메뉴가 "&lt;key&gt;:&lt;enum 이름&gt;" 연결을 이 배열에 넣는다</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumTableAttribute : Attribute
    {
        public readonly string Key;
        public readonly Type EnumType;
        public EnumTableAttribute(string key, Type enumType) { Key = key; EnumType = enumType; }
    }

    /// <summary>문구 가져오기 메뉴가 "&lt;key&gt;:&lt;id&gt;" 연결을 이 목록에 넣는다</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class IdTableAttribute : Attribute
    {
        public readonly string Key;
        public IdTableAttribute(string key) { Key = key; }
    }

    /// <summary>데이터 id ↔ 문구</summary>
    [Serializable]
    public struct IdText
    {
        public string id;
        public LocalizedString text;
    }

    /// <summary>
    /// 화면 문구 참조 묶음 (D6) — Core 데이터 id·enum ↔ String Table 항목.
    /// 문구 가져오기 메뉴(BBGame/Data/문구 가져오기)가 원문 목록(strings_ko.json)의 bind대로 채운다. 손으로 고치지 않는다.
    /// 숫자 키를 코드에 쓰지 않기 위한 자리이기도 하다 — 코드는 이 에셋의 이름 있는 칸으로만 문구를 찾는다
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Presentation/Display Catalog")]
    public sealed class DisplayCatalog : ScriptableObject
    {
        [Header("데이터 이름 (id로 찾는다)")]
        [IdTable("card")] [SerializeField] internal IdText[] _cardNames = Array.Empty<IdText>();
        [IdTable("batter")] [SerializeField] internal IdText[] _batterNames = Array.Empty<IdText>();
        [IdTable("position")] [SerializeField] internal IdText[] _positions = Array.Empty<IdText>();
        [IdTable("school")] [SerializeField] internal IdText[] _schoolNames = Array.Empty<IdText>();
        [IdTable("ourBatter")] [SerializeField] internal IdText[] _ourBatters = Array.Empty<IdText>();
        [IdTable("bullpen")] [SerializeField] internal IdText[] _bullpen = Array.Empty<IdText>();
        [Tooltip("반 이닝 결과 한 줄 — id \"r{점수}.{번호}\", 4점 이상은 \"r4\"")]
        [IdTable("offense")] [SerializeField] internal IdText[] _offenseLines = Array.Empty<IdText>();

        [Header("enum 순서 표")]
        [EnumTable("zone", typeof(ZoneId))] [SerializeField] internal LocalizedString[] _zones;
        [EnumTable("pitchType", typeof(PitchType))] [SerializeField] internal LocalizedString[] _pitchTypes;
        [EnumTable("special", typeof(CardSpecial))] [SerializeField] internal LocalizedString[] _specials;
        [EnumTable("intent", typeof(Intent))] [SerializeField] internal LocalizedString[] _intents;
        [EnumTable("outcome", typeof(PitchOutcome))] [SerializeField] internal LocalizedString[] _outcomes;
        [EnumTable("stab", typeof(Stab))] [SerializeField] internal LocalizedString[] _stabs;
        [EnumTable("stabDesc", typeof(Stab))] [SerializeField] internal LocalizedString[] _stabDescs;
        [EnumTable("read", typeof(ReadVerdict))] [SerializeField] internal LocalizedString[] _reads;
        [EnumTable("kind", typeof(BatterKind))] [SerializeField] internal LocalizedString[] _kinds;
        [EnumTable("rule", typeof(BatterKind))] [SerializeField] internal LocalizedString[] _rules;
        [EnumTable("weather", typeof(Weather))] [SerializeField] internal LocalizedString[] _weathers;
        [EnumTable("condition", typeof(Condition))] [SerializeField] internal LocalizedString[] _conditions;
        [EnumTable("staminaTier", typeof(StaminaLevel))] [SerializeField] internal LocalizedString[] _staminaTiers;
        [EnumTable("gaugeStatus", typeof(GaugeStatus))] [SerializeField] internal LocalizedString[] _gaugeStatus;
        [EnumTable("exitReason", typeof(ExitReason))] [SerializeField] internal LocalizedString[] _exitReasons;
        [EnumTable("targetNote", typeof(TargetNote))] [SerializeField] internal LocalizedString[] _targetNotes;
        [EnumTable("plate", typeof(PlateOrdinal))] [SerializeField] internal LocalizedString[] _plates;
        [EnumTable("bonus", typeof(BonusId))] [SerializeField] internal LocalizedString[] _bonuses;
        [EnumTable("bonusDesc", typeof(BonusId))] [SerializeField] internal LocalizedString[] _bonusDescs;
        [EnumTable("evolution", typeof(Evolution))] [SerializeField] internal LocalizedString[] _evolutions;
        [EnumTable("evolutionDesc", typeof(Evolution))] [SerializeField] internal LocalizedString[] _evolutionDescs;
        [EnumTable("evolutionReq", typeof(Evolution))] [SerializeField] internal LocalizedString[] _evolutionReqs;
        [EnumTable("bullpenState", typeof(BullpenState))] [SerializeField] internal LocalizedString[] _bullpenStates;
        [EnumTable("result", typeof(MatchResult))] [SerializeField] internal LocalizedString[] _results;

        [Header("화면 문구 (ui:필드 이름)")]
        [SerializeField] internal UiTexts _ui = new UiTexts();

        /// <summary>이름 있는 화면 문구</summary>
        public UiTexts Ui => _ui;

        public LocalizedString Zone(ZoneId z) => At(_zones, (int)z);
        public LocalizedString PitchTypeName(PitchType t) => At(_pitchTypes, (int)t);
        public LocalizedString Special(CardSpecial s) => At(_specials, (int)s);
        public LocalizedString IntentName(Intent i) => At(_intents, (int)i);
        public LocalizedString Outcome(PitchOutcome o) => At(_outcomes, (int)o);
        public LocalizedString StabName(Stab s) => At(_stabs, EnumIndex.Of(s));
        public LocalizedString StabDesc(Stab s) => At(_stabDescs, EnumIndex.Of(s));
        public LocalizedString Read(ReadVerdict r) => At(_reads, (int)r);
        public LocalizedString Kind(BatterKind k) => At(_kinds, (int)k);
        public LocalizedString Rule(BatterKind k) => At(_rules, (int)k);
        public LocalizedString WeatherName(Weather w) => At(_weathers, (int)w);
        /// <summary>컨디션 하나 (플래그 한 개)</summary>
        public LocalizedString ConditionName(Condition single) => At(_conditions, EnumIndex.OfFlag((int)single));
        public LocalizedString StaminaTier(StaminaLevel l) => At(_staminaTiers, (int)l);
        public LocalizedString Gauge(GaugeStatus g) => At(_gaugeStatus, (int)g);
        public LocalizedString Exit(ExitReason r) => At(_exitReasons, (int)r);
        /// <summary>목표치 보정 하나 (플래그 한 개)</summary>
        public LocalizedString TargetNoteText(TargetNote single) => At(_targetNotes, EnumIndex.OfFlag((int)single));
        public LocalizedString Plate(PlateOrdinal p) => At(_plates, (int)p);

        public LocalizedString BonusName(BonusId b) => At(_bonuses, (int)b);
        public LocalizedString BonusDesc(BonusId b) => At(_bonusDescs, (int)b);
        /// <summary>진화 하나 (플래그 한 개)</summary>
        public LocalizedString EvolutionName(Evolution single) => At(_evolutions, EnumIndex.OfFlag((int)single));
        public LocalizedString EvolutionDesc(Evolution single) => At(_evolutionDescs, EnumIndex.OfFlag((int)single));
        public LocalizedString EvolutionReq(Evolution single) => At(_evolutionReqs, EnumIndex.OfFlag((int)single));
        public LocalizedString BullpenStateName(BullpenState s) => At(_bullpenStates, (int)s);
        public LocalizedString Result(MatchResult r) => At(_results, (int)r);
        public LocalizedString OurBatterName(string seedKey) => Find(_ourBatters, seedKey);
        public LocalizedString BullpenName(string seedKey) => Find(_bullpen, seedKey);
        public LocalizedString OffenseLine(string id) => Find(_offenseLines, id);

        public LocalizedString CardName(string cardId) => Find(_cardNames, cardId);
        public LocalizedString BatterName(string seedKey) => Find(_batterNames, seedKey);
        public LocalizedString Position(string seedKey) => Find(_positions, seedKey);
        public LocalizedString SchoolName(string schoolId) => Find(_schoolNames, schoolId);

        private static LocalizedString At(LocalizedString[] table, int index)
            => table != null && index >= 0 && index < table.Length ? table[index] : null;

        // 수십 개 선형 탐색 — 화면 갱신 때만 부르고 할당이 없다
        private static LocalizedString Find(IdText[] list, string id)
        {
            for (int i = 0; i < list.Length; i++)
                if (string.Equals(list[i].id, id, StringComparison.Ordinal)) return list[i].text;
            return null;
        }
    }

    /// <summary>
    /// enum 값 → 표 칸 번호. 가져오기 메뉴와 화면이 같은 규칙을 쓴다.
    /// Stab은 −1부터라 +1, 플래그 enum은 None = 0, 그 뒤 비트 순서
    /// </summary>
    public static class EnumIndex
    {
        public static int Of(Stab s) => (int)s + 1;

        /// <summary>플래그 하나의 칸 — None 0, 1 → 1, 2 → 2, 4 → 3 …</summary>
        public static int OfFlag(int bits)
        {
            if (bits == 0) return 0;
            int i = 1;
            while ((bits & 1) == 0) { bits >>= 1; i++; }
            return i;
        }

        /// <summary>가져오기 메뉴용 — enum 종류에 맞는 규칙으로</summary>
        public static int OfBoxed(Type enumType, object value)
        {
            if (enumType == typeof(Stab)) return Of((Stab)value);
            if (enumType.IsDefined(typeof(FlagsAttribute), false)) return OfFlag(Convert.ToInt32(value));
            return Convert.ToInt32(value);
        }

        /// <summary>표 크기 — 값 개수 (Stab 3, 플래그는 None 포함)</summary>
        public static int Size(Type enumType) => Enum.GetValues(enumType).Length;
    }

    /// <summary>이름 있는 화면 문구 — 가져오기 메뉴가 "ui:필드 이름" 연결로 채운다</summary>
    [Serializable]
    public sealed class UiTexts
    {
        [Header("칸 제목 · 버튼")]
        public LocalizedString pitchTitle, todayTitle, staminaTitle, focusTitle, gaugeTitle, scoutTitle, personaLabel, featureLabel;
        public LocalizedString timeButton, ibbButton, signButton, throwButton;
        public LocalizedString runsHeader, hitsHeader, errorsHeader, ballsLabel, strikesLabel, outsLabel;

        [Header("타자 패널 · 게이지")]
        public LocalizedString batterHeader, batterInfo, batsRight, batsLeft, seasonLine, unknown, featureUnknown;
        public LocalizedString gaugeTarget, targetNotes, plateNth;

        [Header("투구 · 스태미나 · 집중력 · 오늘")]
        public LocalizedString restNext, restLimit, restOnly, focusRegen, umpireAbs, umpireHuman;

        [Header("손패 · 던지기")]
        public LocalizedString cardStats, cardFixedType, coursePower, courseFixed, zoneAnywhere;
        public LocalizedString throwPreview, throwCost, throwHint;

        [Header("리빌")]
        public LocalizedString intentLabel, stabLine, switchFailed, gainLine, gainLineBall, gaugeLine, gaugeBall, gaugeHbp;
        public LocalizedString resultLine, tagRuns, noteShaken, notePower, noteCenter, noteCourseCap, noteEyeUp;
        public LocalizedString noteBallKept, noteBallLost, noteCut, noteBenchVisit;
        public LocalizedString nextPitch, nextBatter;

        [Header("안내 (M2 임시)")]
        public LocalizedString nextInning, restart, halfOverTitle, halfOverBody, exitTitle, endTitle, endBody;

        [Header("이닝 보너스 · 우리 공격 · 결과 (M3)")]
        public LocalizedString bonusTitle, bonusLevel, evolutionTag, bottomTitle, seeResult;
        public LocalizedString bullpenChip, bullpenCount, bonusLevelChip, pitcherLine, reliefRuns, reliefHold;

        [Header("공통")]
        public LocalizedString listSeparator, courseSeparator, pitcherName;
    }
}
