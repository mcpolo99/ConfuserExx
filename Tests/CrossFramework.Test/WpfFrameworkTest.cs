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

		// --- Modern .NET: actually run the obfuscated WPF app (shows a real window off-screen,
		//     pumps the dispatcher, self-closes after ~2s) and assert it starts without crashing. ---

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net10.0-windows")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WPF_Net10_SelfTest_RunsWithoutCrashing() =>
			Run("CrossFramework.WPF.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net10-selftest",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunDotnetApp(outputPath, "CrossFramework.WPF.Net10.dll", "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net10.0-windows")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WPF_Net10_SelfTest_ReportsCrash() =>
			Run("CrossFramework.WPF.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net10-crash",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, _) = RunDotnetApp(outputPath, "CrossFramework.WPF.Net10.dll", "--selftest-crash");
					Assert.NotEqual(42, exit);
					Assert.Contains("CRASH", stdout);
					return Task.CompletedTask;
				});

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net6.0-windows")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WPF_Net6_SelfTest_RunsWithoutCrashing() =>
			Run("CrossFramework.WPF.Net6.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net6-selftest",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunDotnetApp(outputPath, "CrossFramework.WPF.Net6.dll", "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WPF")]
		[Trait("TFM", "net8.0-windows")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WPF_Net8_SelfTest_RunsWithoutCrashing() =>
			Run("CrossFramework.WPF.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-wpf-net8-selftest",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunDotnetApp(outputPath, "CrossFramework.WPF.Net8.dll", "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});
	}
}
