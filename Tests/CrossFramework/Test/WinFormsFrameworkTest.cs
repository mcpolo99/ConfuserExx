using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace CrossFramework.Test {
	public class WinFormsFrameworkTest : TestBase {
		public WinFormsFrameworkTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net35")]
		public Task WinForms_Net35_RenameProtection() =>
			Run("CrossFramework.WinForms.Net35.exe",
				new[] { "Label: Not clicked", "Title: ConfuserEx WinForms Test (net35)" },
				new SettingItem<Protection>("rename"),
				processArguments: "--verify",
				outputDirSuffix: "-winforms-net35");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net40")]
		public Task WinForms_Net40_RenameProtection() =>
			Run("CrossFramework.WinForms.Net40.exe",
				new[] { "Label: Not clicked", "Title: ConfuserEx WinForms Test (net40)" },
				new SettingItem<Protection>("rename"),
				processArguments: "--verify",
				outputDirSuffix: "-winforms-net40");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net48")]
		public Task WinForms_Net48_RenameProtection() =>
			Run("CrossFramework.WinForms.Net48.exe",
				new[] { "Label: Not clicked", "Title: ConfuserEx WinForms Test (net48)" },
				new SettingItem<Protection>("rename"),
				processArguments: "--verify",
				outputDirSuffix: "-winforms-net48");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net6.0-windows")]
		public Task WinForms_Net6_RenameProtection() =>
			Run("CrossFramework.WinForms.Net6.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net6",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net8.0-windows")]
		public Task WinForms_Net8_RenameProtection() =>
			Run("CrossFramework.WinForms.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net8",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net10.0-windows")]
		public Task WinForms_Net10_RenameProtection() =>
			Run("CrossFramework.WinForms.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net10",
				checkOutput: false);

		// --- Actually run the obfuscated UI app on every framework: it shows its real window
		//     off-screen, pumps the message loop, self-closes after ~2s (exit 42), and an induced
		//     unhandled exception is caught by the crash handlers and reported as failure. ---

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net35"), Trait("Issue", "103")]
		public Task WinForms_Net35_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WinForms.Net35.exe", "-winforms-net35-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net35"), Trait("Issue", "103")]
		public Task WinForms_Net35_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WinForms.Net35.exe", "-winforms-net35-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net40"), Trait("Issue", "103")]
		public Task WinForms_Net40_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WinForms.Net40.exe", "-winforms-net40-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net40"), Trait("Issue", "103")]
		public Task WinForms_Net40_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WinForms.Net40.exe", "-winforms-net40-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net48"), Trait("Issue", "103")]
		public Task WinForms_Net48_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WinForms.Net48.exe", "-winforms-net48-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net48"), Trait("Issue", "103")]
		public Task WinForms_Net48_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WinForms.Net48.exe", "-winforms-net48-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net6.0"), Trait("Issue", "103")]
		public Task WinForms_Net6_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WinForms.Net6.dll", "-winforms-net6-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net6.0"), Trait("Issue", "103")]
		public Task WinForms_Net6_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WinForms.Net6.dll", "-winforms-net6-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net8.0"), Trait("Issue", "103")]
		public Task WinForms_Net8_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WinForms.Net8.dll", "-winforms-net8-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net8.0"), Trait("Issue", "103")]
		public Task WinForms_Net8_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WinForms.Net8.dll", "-winforms-net8-crash");

		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net10.0"), Trait("Issue", "103")]
		public Task WinForms_Net10_SelfTest_Runs() => SelfTestRunsWithoutCrashing("CrossFramework.WinForms.Net10.dll", "-winforms-net10-selftest");
		[Fact, Trait("Category", "CrossFramework"), Trait("AppType", "WinForms"), Trait("TFM", "net10.0"), Trait("Issue", "103")]
		public Task WinForms_Net10_SelfTest_Crash() => SelfTestReportsCrash("CrossFramework.WinForms.Net10.dll", "-winforms-net10-crash");
	}
}
