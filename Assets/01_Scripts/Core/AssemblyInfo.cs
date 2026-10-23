using System.Runtime.CompilerServices;

// 데이터 SO의 internal 세터는 에디터 가져오기와 테스트만 쓴다 (public API는 읽기 전용)
[assembly: InternalsVisibleTo("BBGame.Editor")]
[assembly: InternalsVisibleTo("BBGame.Tests.EditMode")]
