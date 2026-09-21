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
		[Trait("TFM", "net6.0-windows")]
		public Task WPF_Net6_RenameProtection() =>
			Run("CrossFramework.WPF.Net6.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net6",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net8.0-windows")]
		public Task WPF_Net8_RenameProtection() =>
			Run("CrossFramework.WPF.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net8",
				checkOutput: false);

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

		// --- Actually run the obfuscated WPF app on every framework: it shows its real window
		//     off-screen/invisible, pumps the dispatcher, self-terminates (exit 42), and an induced
		//     unhandled exception is caught by the crash handlers and reported as failure. ---

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net35"), Trait("Issue", "103")]
		public Task WPF_Net35_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WPF.Net35.exe", "-wpf-net35-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net35"), Trait("Issue", "103")]
		public Task WPF_Net35_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WPF.Net35.exe", "-wpf-net35-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net40"), Trait("Issue", "103")]
		public Task WPF_Net40_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WPF.Net40.exe", "-wpf-net40-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net40"), Trait("Issue", "103")]
		public Task WPF_Net40_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WPF.Net40.exe", "-wpf-net40-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net48"), Trait("Issue", "103")]
		public Task WPF_Net48_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WPF.Net48.exe", "-wpf-net48-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net48"), Trait("Issue", "103")]
		public Task WPF_Net48_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WPF.Net48.exe", "-wpf-net48-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net6.0-windows"), Trait("Issue", "103")]
		public Task WPF_Net6_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WPF.Net6.dll", "-wpf-net6-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net6.0-windows"), Trait("Issue", "103")]
		public Task WPF_Net6_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WPF.Net6.dll", "-wpf-net6-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net8.0-windows"), Trait("Issue", "103")]
		public Task WPF_Net8_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WPF.Net8.dll", "-wpf-net8-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net8.0-windows"), Trait("Issue", "103")]
		public Task WPF_Net8_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WPF.Net8.dll", "-wpf-net8-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net10.0-windows"), Trait("Issue", "103")]
		public Task WPF_Net10_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WPF.Net10.dll", "-wpf-net10-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WPF"), Trait("TFM", "net10.0-windows"), Trait("Issue", "103")]
		public Task WPF_Net10_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WPF.Net10.dll", "-wpf-net10-crash");
	}
}
