using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace AntiTamper.Test {
	public sealed class AntiTamperTest : TestBase {
		public AntiTamperTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Theory]
		[InlineData("normal")]
		[InlineData("anti")]
		[Trait("Category", "Protection")]
		[Trait("Protection", "anti tamper")]
		public Task ProtectAntiTamperAndExecute(string antiTamperMode) {
			return Run("AntiTamper.exe",
				new[] { "This is a test." },
				new SettingItem<Protection>("anti tamper") { { "mode", antiTamperMode } },
				"_" + antiTamperMode);
		}
	}
}
