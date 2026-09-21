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

		// --- Modern .NET: actually run the obfuscated UI app (shows a real window off-screen,
		//     pumps the message loop, self-closes after ~2s) and assert it starts without crashing. ---

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net10.0")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WinForms_Net10_SelfTest_RunsWithoutCrashing() =>
			Run("CrossFramework.WinForms.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net10-selftest",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunDotnetApp(outputPath, "CrossFramework.WinForms.Net10.dll", "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net10.0")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WinForms_Net10_SelfTest_ReportsCrash() =>
			Run("CrossFramework.WinForms.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net10-crash",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, _) = RunDotnetApp(outputPath, "CrossFramework.WinForms.Net10.dll", "--selftest-crash");
					Assert.NotEqual(42, exit);
					Assert.Contains("CRASH", stdout);
					return Task.CompletedTask;
				});

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net6.0")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WinForms_Net6_SelfTest_RunsWithoutCrashing() =>
			Run("CrossFramework.WinForms.Net6.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net6-selftest",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunDotnetApp(outputPath, "CrossFramework.WinForms.Net6.dll", "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "WinForms")]
		[Trait("TFM", "net8.0")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/103")]
		public Task WinForms_Net8_SelfTest_RunsWithoutCrashing() =>
			Run("CrossFramework.WinForms.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-winforms-net8-selftest",
				checkOutput: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunDotnetApp(outputPath, "CrossFramework.WinForms.Net8.dll", "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});
	}
}
