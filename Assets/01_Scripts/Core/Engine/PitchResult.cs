namespace BBGame.Core
{
    /// <summary>특수 카드 착지 (리빌 부가 장면)</summary>
    public enum LandingKind : byte { Normal, PowerPitch, CenterAim }

    /// <summary>
    /// 한 투구의 결과 — Core가 채우고 Presentation이 리빌에 쓴다 (§21, D3).
    /// 경기마다 한 개를 재사용한다 — 리빌이 끝나기 전에는 다음 Throw를 받지 않는다 (화면 입력 잠금).
    /// 리빌 동안 화면은 "던지기 전" 값(…Before)과 "던진 뒤" 값(…After)을 함께 그린다.
    /// </summary>
    public sealed class PitchResult
    {
        // 무엇을 던졌나
        public CardInstance Card;
        public int HandIndex;
        public int PitchNumber;            // 이번 타석 몇 번째 공
        public BatterState Batter;

        // 착지·제구 (§11-3)
        public Zone Intended;
        public Zone Actual;
        public bool Shaken;
        public LandingKind Landing;
        public int Velocity;               // 화면 구속 — 던진 뒤에만 공개 (§4-6)

        // 판정 — 실제 칸 기준 (§6-2, §8-4)
        public Intent BatterIntent;        // 리빌에서 공개
        public bool SwitchFailed;
        public Intent? Declared;
        public Stab Stab;
        public ReadState ReadState;
        public double ReadMultiplier;      // 리빌에서 처음 공개
        public bool CourseCapped;

        // 제압치 (§8-1)
        public int BaseShown;              // 실제 칸 기준 기본 제압치 (화면 값)
        public int BaseIfIntended;         // 흔들렸으면 의도 칸 기준 값 (v11), 아니면 BaseShown
        public double AddShown;
        public int Gain;
        public bool BallKept;              // 찔렀다 볼 40% 적립
        public int GaugeBefore, GaugeAfter;
        public int TargetBefore, TargetAfter;   // 선구안형은 볼마다 T가 오른다
        public bool ReachedTarget;

        // 반응·결과 (§9, §10)
        public Reaction Reaction;
        public bool Chased;                // 존 밖 공을 쫓았다
        public PitchOutcome Outcome;
        public bool Grounder;              // 인플레이 타구가 땅볼
        public bool Bloop;                 // 제압 상태 바가지
        public bool SacFly;
        public Count CountBefore, CountAfter;
        public BaseMask BasesBefore, BasesAfter;
        public int OutsBefore, OutsAfter;
        public int RunsScored;
        public int StaminaAfter;
        public int MentalAfter;
        public bool AtBatEnded;
        public bool HalfInningEnded;
        public bool PitcherExited;
        public ExitReason ExitReason;
        public bool BenchVisit;            // 투수 코치 방문 (벤치 판단 1차)

        internal void Reset()
        {
            Card = null; Batter = null; HandIndex = PitchNumber = 0;
            Shaken = false; Landing = LandingKind.Normal; Velocity = 0;
            SwitchFailed = false; Declared = null; CourseCapped = false; ReadMultiplier = 0;
            BaseShown = BaseIfIntended = Gain = 0; AddShown = 0; BallKept = false;
            GaugeBefore = GaugeAfter = TargetBefore = TargetAfter = 0; ReachedTarget = false;
            Chased = Grounder = Bloop = SacFly = false;
            OutsBefore = OutsAfter = RunsScored = StaminaAfter = MentalAfter = 0;
            AtBatEnded = HalfInningEnded = PitcherExited = BenchVisit = false;
            ExitReason = ExitReason.None;
        }
    }
}
