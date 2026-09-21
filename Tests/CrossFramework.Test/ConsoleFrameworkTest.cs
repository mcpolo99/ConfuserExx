using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace CrossFramework.Test {
	public class ConsoleFrameworkTest : TestBase {
		public ConsoleFrameworkTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		// --- .NET Framework (produces real .exe — obfuscate + run) ---

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net20")]
		public Task Console_Net20_RenameProtection() =>
			Run("CrossFramework.Console.Net20.exe",
				new[] { "Hello from net20" },
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net20");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net35")]
		public Task Console_Net35_RenameProtection() =>
			Run("CrossFramework.Console.Net35.exe",
				new[] { "Hello from net35" },
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net35");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net40")]
		public Task Console_Net40_RenameProtection() =>
			Run("CrossFramework.Console.Net40.exe",
				new[] { "Hello from net40" },
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net40");

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net48")]
		public Task Console_Net48_RenameProtection() =>
			Run("CrossFramework.Console.Net48.exe",
				new[] { "Hello from net48" },
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net48");

		// --- Modern .NET (apphost .exe is not a managed assembly — obfuscate the .dll) ---

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net6.0")]
		public Task Console_Net6_RenameProtection() =>
			Run("CrossFramework.Console.Net6.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net6",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net8.0")]
		public Task Console_Net8_RenameProtection() =>
			Run("CrossFramework.Console.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net8",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net10.0")]
		public Task Console_Net10_RenameProtection() =>
			Run("CrossFramework.Console.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-console-net10",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Console")]
		[Trait("TFM", "net10.0")]
		[Trait("Protection", "resources")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/101")]
		public Task Console_Net10_ResourceProtection() =>
			Run(new[] { "CrossFramework.Console.Net10.dll", "external:CrossFramework.Console.Net10.runtimeconfig.json" },
				null,
				new SettingItem<Protection>("resources"),
				outputDirSuffix: "-console-net10-resources",
				checkOutput: false,
				postProcessAction: outputPath => {
					var startInfo = new ProcessStartInfo("dotnet", "CrossFramework.Console.Net10.dll") {
						WorkingDirectory = outputPath,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false
					};
					using var process = Process.Start(startInfo);
					var output = process.StandardOutput.ReadToEnd();
					var error = process.StandardError.ReadToEnd();
					process.WaitForExit();
					Assert.Equal(42, process.ExitCode);
					Assert.Empty(error);
					Assert.Equal(new[] { "START", "Hello from net10.0", "Resource from net10.0", "END" },
						output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
					return Task.CompletedTask;
				});
	}
}
