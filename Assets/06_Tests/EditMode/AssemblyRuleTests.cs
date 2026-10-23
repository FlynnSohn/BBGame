using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>
    /// 어셈블리 규칙 검사 (decisions 2026-10-13 코드 구조·텍스트).
    /// </summary>
    public class AssemblyRuleTests
    {
        // Core가 쓰면 안 되는 어셈블리.
        // Core는 동기·결정적인 순수 로직이고, 문구는 원인 태그만 내보낸다 (Localization 금지)
        private static readonly string[] k_ForbiddenForCore =
        {
            "Unity.Localization",
            "UniTask",
            "PrimeTween.Runtime",
            "Unity.InputSystem",
            "Unity.TextMeshPro",
            "UnityEngine.UI",
        };

        /// <summary>
        /// Core 코드가 실제로 쓰는 어셈블리에 금지 목록이 없는지 확인한다.
        /// GetReferencedAssemblies()는 코드에서 실제로 쓰는 어셈블리만 돌려주므로,
        /// "Core 코드가 그 어셈블리를 쓰기 시작한 순간"을 잡는다.
        /// </summary>
        [Test]
        public void Core_DoesNotUseForbiddenAssemblies()
        {
            var referenced = typeof(BBGame.Core.CoreAssembly).Assembly.GetReferencedAssemblies();
            foreach (var asm in referenced)
                CollectionAssert.DoesNotContain(k_ForbiddenForCore, asm.Name, $"Core가 {asm.Name}을(를) 참조한다");
        }
    }
}
