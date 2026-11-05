using System.Runtime.CompilerServices;

// 데이터 SO의 internal 세터는 에디터 가져오기와 테스트만 쓴다 (public API는 읽기 전용)
[assembly: InternalsVisibleTo("BBGame.Editor")]
[assembly: InternalsVisibleTo("BBGame.Tests.EditMode")]
// 수치 검토 봇 — 엔진 난수·이번 공 노림을 읽는 봇 전용 함수(MatchEngine.Bot.cs)는 이 어셈블리만 쓴다. 화면(Presentation)에는 열지 않는다 (§20-4)
[assembly: InternalsVisibleTo("BBGame.Bots")]
