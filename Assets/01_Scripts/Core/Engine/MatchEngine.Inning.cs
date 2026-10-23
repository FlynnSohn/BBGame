namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        /// <summary>
        /// 이닝 보너스를 고른다 (§13) — 적용한 뒤 우리 공격(회말)과 다음 이닝 첫 공 직전까지, 또는 경기 끝까지 한 번에 진행한다
        /// (프로토타입 chooseBonus → bottomHalf). 고를 수 없는 번호면 아무 일도 없다
        /// </summary>
        public void ChooseBonus(int offerIndex)
        {
            if (_s.Phase != MatchPhase.BonusPick || offerIndex < 0 || offerIndex >= _s.OffersList.Count) return;
            ApplyOffer(_s.OffersList[offerIndex]);
            _s.OffersList.Clear();
            BottomHalf();
        }

        /// <summary>반 이닝 종료 (프로토타입 endTop) — 주자를 비우고 보너스 후보를 뽑는다. (M7) 만루 탈출·경제적 이닝</summary>
        private void EndTop()
        {
            ClearBases();
            _s.Phase = MatchPhase.BonusPick;
            if (_db.BonusTable != null) BonusOffers.Generate(_db.BonusTable, _f, _s.Modifiers, ref _rng, _s.OffersList);
            else _s.OffersList.Clear();
            if (_s.OffersList.Count == 0) BottomHalf();   // 고를 것이 없으면 바로 진행 (보너스가 모두 최대 레벨)
        }

        /// <summary>보너스 레벨 +1 · 진화 · 페이스 조절 즉시 회복 (경기 시작 최대치 기준, v10 W3)</summary>
        private void ApplyOffer(in BonusOffer offer)
        {
            var m = _s.Modifiers;
            if (offer.Kind == OfferKind.Evolution)
            {
                m.Evolutions |= offer.Evolution;
                return;
            }
            m.SetLevel(offer.Bonus, m.Level(offer.Bonus) + 1);
            if (offer.Bonus == BonusId.Pace)
            {
                var st = _t.stamina;
                _s.StaminaMax += JsMath.RoundHalfUp(_s.StaminaMaxAtStart * st.paceMaxPct);
                _s.Stamina = System.Math.Min(_s.StaminaMax, _s.Stamina + JsMath.RoundHalfUp(_s.StaminaMaxAtStart * st.pacePct));
            }
        }

        /// <summary>
        /// 회말 (프로토타입 bottomHalf, §14) — 9회에 앞서 있으면 X로 끝. 아니면 우리 공격 반 이닝, 9회면 경기 끝, 아니면 다음 이닝 초
        /// </summary>
        private void BottomHalf()
        {
            int i = _s.Inning;
            int innings = _t.game.innings;
            if (i >= innings && _s.ScoreUs > _s.ScoreOpp)
            {
                _s.LineUs[i - 1] = -1;   // X
                _s.LineUsPlayed = i;
                EndGame();
                return;
            }
            PlayUsHalf(PitcherOfRecord.Me);
            if (i >= innings) { EndGame(); return; }

            _s.Inning++;
            _s.Outs = 0;
            ClearBases();
            _s.LineOpp[_s.Inning - 1] = 0;
            _s.InningRuns = 0;
            _s.HitStreak = 0;
            _s.WalkStreak = 0;
            // (M6) 관찰력 무료 뒤집기 · (M7) 만루 탈출 집중력
            _s.Phase = MatchPhase.Pitching;
            StartAtBat();
        }

        /// <summary>우리 공격 반 이닝 — 점수·라인스코어·장부·중계 번호 (안타는 지금 0 — 사용자 결정 2026-10-24)</summary>
        private void PlayUsHalf(PitcherOfRecord por)
        {
            var o = _t.offense;
            int runs = HalfInningSim.Runs(o, ref _rng, o.teamOff, o.oppTeam, 1);
            AddUsRuns(runs, por);
            int names = OurBatterCount;
            HalfInningSim.PickLine(ref _rng, runs, names, out int a, out int b, out int line);
            _s.HalfLogList.Add(new HalfInningRecord(HalfKind.UsOffense, _s.Inning, runs, a, b, line, 0));
        }

        /// <summary>우리 득점 (프로토타입 addUsRuns) — 0점이어도 라인스코어 칸을 채운다</summary>
        private void AddUsRuns(int runs, PitcherOfRecord por)
        {
            _s.LineUs[_s.Inning - 1] = runs;
            _s.LineUsPlayed = _s.Inning;
            if (runs <= 0) return;
            _s.ScoreUs += runs;
            _s.LedgerList.Add(new LedgerEntry(true, runs, false, por));
        }

        /// <summary>경기 끝 — 우리 팀 기준 승·패·무 (연장 없음, §14)</summary>
        private void EndGame()
        {
            _s.Phase = MatchPhase.Ended;
            _s.Result = _s.ScoreUs > _s.ScoreOpp ? MatchResult.Win : _s.ScoreUs < _s.ScoreOpp ? MatchResult.Loss : MatchResult.Tie;
        }

        /// <summary>우리 타자 이름 수 — 중계 문구 이름 뽑기 (프로토타입 OUR_BATS 9명)</summary>
        private int OurBatterCount => _db.BonusTable != null && _db.BonusTable.OurBatters.Length > 1 ? _db.BonusTable.OurBatters.Length : 9;
    }
}
