using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace CrossFramework.Test {
	public class WpfFrameworkTest : TestBase {
		public WpfFrameworkTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net35")]
		public Task WPF_Net35_RenameProtection() =>
			Run("CrossFramework.WPF.Net35.exe",
				new[] { "Title: ConfuserEx WPF Test (net35)", "Content: WPF is working" },
				new SettingItem<Protection>("rename"),
				processArguments: "--verify",
				outputDirSuffix: "-wpf-net35");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net40")]
		public Task WPF_Net40_RenameProtection() =>
			Run("CrossFramework.WPF.Net40.exe",
				new[] { "Title: ConfuserEx WPF Test (net40)", "Content: WPF is working" },
				new SettingItem<Protection>("rename"),
				processArguments: "--verify",
				outputDirSuffix: "-wpf-net40");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net48")]
		public Task WPF_Net48_RenameProtection() =>
			Run("CrossFramework.WPF.Net48.exe",
				new[] { "Title: ConfuserEx WPF Test (Net48)", "Content: WPF is working" },
				new SettingItem<Protection>("rename"),
				processArguments: "--verify",
				outputDirSuffix: "-wpf-net48");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net10.0-windows")]
		public Task WPF_Net10_RenameProtection() =>
			Run("CrossFramework.WPF.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net10",
				checkOutput: false);
	}
}
