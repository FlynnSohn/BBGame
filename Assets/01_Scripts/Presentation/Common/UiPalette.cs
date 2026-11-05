using UnityEngine;

namespace BBGame.Presentation.Common
{
    /// <summary>
    /// 화면 색 — 밝은 일러스트 배경용 (match_loop_plan §6-4, Q8). 의미 배정은 §20-6을 지킨다.
    /// 바탕·글자·버튼은 푸른 계열 (reference_2, match_m9_bots_plan 추가 범위 2026-11-05).
    /// View는 색 값을 직접 쓰지 않고 이 에셋의 이름으로만 쓴다 — 시안이 바뀌면 이 에셋만 고친다
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Presentation/UI Palette")]
    public sealed class UiPalette : ScriptableObject
    {
        [Header("의미 색 (큰 글씨·막대 / 작은 글씨)")]
        [Tooltip("플레이어의 읽기 · 유리 · 제압 · 찔렀다")] public Color read = Hex(0x14A38F);
        public Color readText = Hex(0x0B7D6D);
        [Tooltip("타자의 노림 · 불리 · 걸렸다")] public Color intent = Hex(0xD8453C);
        public Color intentText = Hex(0xB5302A);
        [Tooltip("경고 · 목표 미달 · 스트라이크")] public Color warn = Hex(0xE3961A);
        public Color warnText = Hex(0xA86A00);
        [Tooltip("S등급 · 압도 · 진화 · 새 족보")] public Color gold = Hex(0xD4A017);
        public Color goldBorder = Hex(0x9C7400);

        [Header("바탕 · 글자")]
        public Color panel = Hex(0xF4F7FC, 0.93f);
        public Color panelBorder = Hex(0xA8C2E6);
        [Tooltip("타자 이름 머리띠 · 고른 칸·카드 강조")] public Color accent = Hex(0x2F6BC0);
        public Color text = Hex(0x1E2B4F);
        public Color textSub = Hex(0x53658A);
        public Color textDim = Hex(0x97A5BE);
        public Color textOnAccent = Color.white;
        [Tooltip("버튼 바탕 — 남색 (타임·고의사구·사인·던진다·안내 창)")] public Color button = Hex(0x2C4A7C);
        [Tooltip("버튼 테두리")] public Color buttonBorder = Hex(0xA9C8F0);
        [Tooltip("'오늘' 칩 바탕")] public Color chip = Hex(0xD5E3F7);
        [Tooltip("'오늘' 칩 기본 글자")] public Color chipText = Hex(0x24508F);

        [Header("요소")]
        public Color gradeN = Hex(0x2F6BC0);
        [Tooltip("R은 보라 — 레드(타자의 노림)와 겹치지 않게")] public Color gradeR = Hex(0x8B5CF6);
        public Color gradeS = Hex(0xD4A017);
        public Color stamina = Hex(0x4CB944);
        [Tooltip("집중력 — 앰버(경고)와 겹치지 않게 인디고")] public Color focus = Hex(0x5B6CD9);
        public Color barTrack = Hex(0xDDE6F3);
        [Tooltip("볼·스트라이크·아웃 불 꺼짐")] public Color pipOff = Hex(0xCCD7E6);
        public Color ball = Hex(0x14A38F);
        public Color strike = Hex(0xE3961A);
        public Color outPip = Hex(0xD8453C);
        public Color baseOn = Hex(0xE3961A);
        public Color baseOff = Hex(0xFFFFFF, 0.9f);

        [Header("스트라이크 존")]
        [Tooltip("존 안 테두리 — 흰색 (레드는 타자 노림 색이라 쓰지 않는다)")] public Color zoneLine = Color.white;
        [Tooltip("존 밖 칸 테두리")] public Color zoneRingLine = Hex(0xFFFFFF, 0.55f);
        [Tooltip("존 밖 칸 바탕")] public Color zoneIdle = Hex(0xFFFFFF, 0.08f);
        [Tooltip("존 안 칸 바탕")] public Color zoneInsideIdle = Hex(0xFFFFFF, 0.22f);
        [Tooltip("고를 수 있는 칸")] public Color zoneLit = Hex(0x2F6BC0, 0.35f);
        [Tooltip("고른 칸")] public Color zoneSelected = Hex(0x2F6BC0, 0.7f);
        [Tooltip("리빌 — 의도한 칸 (제구 미스)")] public Color zoneIntended = Hex(0xFFFFFF, 0.35f);
        public Color zoneCountText = Hex(0x1E2B4F);

        [Header("손패")]
        public Color card = Hex(0xF7FAFE, 0.97f);
        [Tooltip("코스 먼저 모드에서 그 칸으로 못 던지는 카드의 투명도")] [Range(0, 1)] public float cardDimAlpha = 0.35f;
        public Color cardSelectedFrame = Hex(0x2F6BC0);

        /// <summary>0xRRGGBB → Color</summary>
        public static Color Hex(int rgb, float alpha = 1f)
            => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}
