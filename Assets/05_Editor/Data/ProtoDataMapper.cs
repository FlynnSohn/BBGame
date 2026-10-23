using System;
using System.Collections.Generic;
using BBGame.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BBGame.EditorTools.Data
{
    /// <summary>
    /// 가져오기 JSON → 데이터 SO 값. 에셋 저장(AssetDatabase)과 분리한 순수 매핑이라 테스트·외부 검증에도 쓴다.
    /// 에셋 만들기·찾기는 넘겨받은 팩토리가 한다 — 있으면 기존 에셋을 돌려줘 GUID가 유지된다.
    /// </summary>
    public static class ProtoDataMapper
    {
        /// <summary>에셋 팩토리 — (종류, 이름) → 기존 에셋이나 새 인스턴스</summary>
        public interface IAssetSource
        {
            T Get<T>(string name) where T : ScriptableObject;
        }

        /// <summary>JSON 전체를 SO들에 적용하고 데이터 묶음을 돌려준다</summary>
        public static GameDatabase Apply(ProtoData d, IAssetSource src)
        {
            var db = src.Get<GameDatabase>("Database");

            // 구종 표 — PitchType 순서로 정렬
            var table = src.Get<PitchTypeTable>("PitchTypes");
            var rows = new PitchTypeRow[d.pitchTypes.Count];
            foreach (var p in d.pitchTypes)
            {
                var type = Parse<PitchType>(p.type);
                rows[(int)type] = new PitchTypeRow
                {
                    type = type, family = Parse<PitchFamily>(p.family), like = Parse<PitchLike>(p.like), baseVelo = p.baseVelo, cardOnly = p.cardOnly,
                    groundBonus = p.groundBonus, whiffMul = p.whiffMul, whiffSameMul = p.whiffSameMul, whiffOppMul = p.whiffOppMul,
                    hitOppMul = p.hitOppMul, jamOppMul = p.jamOppMul, whiffLowMul = p.whiffLowMul, shakeAdd = p.shakeAdd
                };
            }
            table.Rows = rows;
            db.PitchTypes = table;

            // 카드
            var cards = new List<CardDef>(d.cards.Count);
            var cardById = new Dictionary<string, CardDef>();
            foreach (var c in d.cards)
            {
                var a = src.Get<CardDef>("Cards/" + c.id);
                a.Id = c.id;
                a.PitchType = Parse<PitchType>(c.pitchType);
                a.Grade = Parse<Grade>(c.grade);
                a.Special = Parse<CardSpecial>(c.special);
                var zones = new ZoneId[c.zones.Count];
                for (int i = 0; i < zones.Length; i++)
                {
                    if (!Zone.TryParsePrototype(c.zones[i], out var z)) throw new FormatException($"카드 {c.id}: 칸 '{c.zones[i]}'을 읽지 못했다");
                    zones[i] = z.Id;
                }
                a.Zones = zones;
                a.IsPowerPitch = c.isPowerPitch; a.IsFixed = c.isFixed; a.IsTemporary = c.isTemporary;
                a.StuffAdjust = c.stuffAdjust; a.CostAdjust = c.costAdjust; a.VeloAdjust = c.veloAdjust;
                cards.Add(a);
                cardById[c.id] = a;
            }
            db.Cards = cards.ToArray();
            db.StarterDeck = d.starterDeck.ConvertAll(id => cardById[id]).ToArray();
            db.FixedCard = cardById[d.fixedCard];
            db.TodayCard = cardById[d.todayCard];

            // 타자 유형
            var typeByKey = new Dictionary<string, BatterTypeDef>();
            var types = new List<BatterTypeDef>();
            foreach (var t in d.batterTypes)
            {
                var a = src.Get<BatterTypeDef>("BatterTypes/" + t.key);
                a.Kind = Parse<BatterKind>(t.kind);
                a.BaseTarget = t.baseTarget; a.Adjust = t.adjust; a.Power = t.power; a.Chase = t.chase;
                a.CanBunt = t.canBunt; a.IsCourse = t.isCourse;
                a.IntentWeights = t.weights.ToArray();
                typeByKey[t.key] = a;
                types.Add(a);
            }
            db.BatterTypes = types.ToArray();

            // 학교·타자
            var schools = new List<SchoolDef>();
            foreach (var s in d.schools)
            {
                var school = src.Get<SchoolDef>("Schools/" + s.id);
                school.Id = s.id;
                school.IsPractice = s.isPractice;
                var lineup = new BatterDef[s.lineup.Count];
                for (int i = 0; i < lineup.Length; i++)
                {
                    var x = s.lineup[i];
                    var a = src.Get<BatterDef>($"Batters/{s.id}_{x.number}");
                    a.SeedKey = x.seedKey;
                    a.Hand = Parse<Hand>(x.hand);
                    a.Type = typeByKey[x.type];
                    if (a.Grade == 0) a.Grade = x.grade;   // 학년은 사람이 고친 값을 덮어쓰지 않는다
                    a.Pull = x.pull; a.Center = x.center; a.Oppo = x.oppo;
                    a.FastballBias = x.fastballBias; a.BreakingBias = x.breakingBias;
                    a.FirstPitch = Parse<FirstPitchStyle>(x.firstPitch);
                    a.TwoStrikeContact = x.twoStrikeContact; a.TwoStrikeChase = x.twoStrikeChase; a.RispFactor = x.rispFactor;
                    a.ZoneHit = x.zoneHit.ToArray();
                    a.ZoneChase = x.zoneChase.ToArray();
                    a.Season = new SeasonRecord { atBats = x.season.atBats, hits = x.season.hits, homeRuns = x.season.homeRuns, rbi = x.season.rbi };
                    lineup[i] = a;
                }
                school.Lineup = lineup;
                schools.Add(school);
            }
            db.Schools = schools.ToArray();

            // 튜닝 — 그룹 이름·필드 이름이 JSON 키와 같다
            var tuning = src.Get<TuningConfig>("Tuning/TuningConfig");
            foreach (var prop in d.tuning.Properties())
            {
                var field = typeof(TuningConfig).GetField(prop.Name);
                if (field == null) throw new FormatException($"튜닝 그룹 '{prop.Name}'이 TuningConfig에 없다");
                var group = field.GetValue(tuning);
                PopulateFields(group, (JObject)prop.Value, prop.Name);
            }
            db.Tuning = tuning;

            // 단계
            var stages = new StageFeatures[d.stages.Count];
            for (int i = 0; i < stages.Length; i++)
            {
                var a = src.Get<StageFeatures>("Stages/Stage" + i);
                PopulateFields(a, d.stages[i], "stage" + i);
                stages[i] = a;
            }
            db.Stages = stages;

            // 이닝 보너스 표 (M3)
            if (d.bonusTable != null)
            {
                var bt = src.Get<BonusTable>("BonusTable");
                bt.Bonuses = d.bonusTable.bonuses.ConvertAll(b => new BonusRow
                {
                    id = Parse<BonusId>(b.id), maxLevel = b.maxLevel, need = Parse<FeatureNeed>(b.need), perLevel = b.perLevel
                }).ToArray();
                bt.Evolutions = d.bonusTable.evolutions.ConvertAll(e => new EvolutionRow
                {
                    evolution = Parse<Evolution>(e.evolution), first = Parse<BonusId>(e.first), firstLevel = e.firstLevel,
                    second = Parse<BonusId>(e.second), secondLevel = e.secondLevel, need = Parse<FeatureNeed>(e.need)
                }).ToArray();
                bt.OurBatters = d.bonusTable.ourBatters.ToArray();
                bt.Bullpen = d.bonusTable.bullpen.ToArray();
                db.BonusTable = bt;
            }
            return db;
        }

        /// <summary>JSON 키 → 같은 이름의 public 필드. 모르는 키는 오류 (옮겨 적기 실수를 잡는다)</summary>
        private static void PopulateFields(object target, JObject values, string where)
        {
            var type = target.GetType();
            foreach (var p in values.Properties())
            {
                var f = type.GetField(p.Name);
                if (f == null) throw new FormatException($"{where}: 필드 '{p.Name}'이 {type.Name}에 없다");
                f.SetValue(target, p.Value.ToObject(f.FieldType));
            }
        }

        private static T Parse<T>(string s) where T : struct, Enum
        {
            if (!Enum.TryParse<T>(s, false, out var v)) throw new FormatException($"{typeof(T).Name}: '{s}'을 읽지 못했다");
            return v;
        }
    }
}
