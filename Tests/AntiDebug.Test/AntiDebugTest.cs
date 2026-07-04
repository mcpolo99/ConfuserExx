using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace AntiDebug.Test {
	public sealed class AntiDebugTest : TestBase {
		public AntiDebugTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		// Regression guard: anti-debug injects a startup check plus a background watchdog into
		// the module cctor. This verifies that an assembly protected with anti-debug still runs
		// normally when it is NOT being debugged — i.e. the strengthened checks (blocking startup
		// check, faster polling) do not false-positive on a plain process launch. The test runner
		// launches the subject as an ordinary child process with no debugger attached, so a healthy
		// build must produce START / <resource> / END and exit code 42.
		[Theory]
		[InlineData("safe")]
		[InlineData("win32")]
		[Trait("Category", "Protection")]
		[Trait("Protection", "anti debug")]
		public Task ProtectAntiDebugAndExecute(string mode) =>
			Run("AntiTamper.exe",
				new[] { "This is a test." },
				new SettingItem<Protection>("anti debug") { { "mode", mode } },
				"_antidebug_" + mode);
	}
}
