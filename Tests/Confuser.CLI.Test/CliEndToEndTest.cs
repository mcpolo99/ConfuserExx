using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Confuser.CLI.Test {
	public class CliEndToEndTest {
		readonly ITestOutputHelper output;

		public CliEndToEndTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void Obfuscate_SampleApp_ProducesRunnableOutput() {
			// Arrange — locate pre-built SampleApp.exe and Confuser.CLI
			var sampleAppExe = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "bin", "SampleApp.exe");
			Assert.True(File.Exists(sampleAppExe), $"Pre-built SampleApp.exe not found at {sampleAppExe}");

			var cliDll = Path.Combine(AppContext.BaseDirectory, "Confuser.CLI.dll");
			Assert.True(File.Exists(cliDll), $"Confuser.CLI.dll not found at {cliDll}");

			var testDir = Path.Combine(Path.GetTempPath(), "confuserex-cli-e2e-" + Guid.NewGuid().ToString("N")[..8]);
			Directory.CreateDirectory(testDir);

			try {
				// Copy the pre-built exe to a working directory
				File.Copy(sampleAppExe, Path.Combine(testDir, "SampleApp.exe"));

				// Write the .crproj
				var outputDir = Path.Combine(testDir, "obfuscated");
				var crproj = Path.Combine(testDir, "SampleApp.crproj");
				File.WriteAllText(crproj,
@"<project outputDir="".\obfuscated"" baseDir=""."" xmlns=""http://confuser.codeplex.com"">
  <rule pattern=""true"" preset=""none"" inherit=""false"">
    <protection id=""rename"" />
  </rule>
  <module path=""SampleApp.exe"" />
</project>");

				// Act — run Confuser.CLI
				var cliResult = RunProcess("dotnet", $"\"{cliDll}\" -n \"{crproj}\"");
				output.WriteLine("=== Confuser.CLI Output ===");
				output.WriteLine(cliResult.stdout);
				if (!string.IsNullOrEmpty(cliResult.stderr))
					output.WriteLine(cliResult.stderr);
				Assert.Equal(0, cliResult.exitCode);

				// Assert — obfuscated output exists
				var obfuscatedExe = Path.Combine(outputDir, "SampleApp.exe");
				Assert.True(File.Exists(obfuscatedExe), "Obfuscated SampleApp.exe should exist");

				// Assert — obfuscated file differs from original
				var originalBytes = File.ReadAllBytes(Path.Combine(testDir, "SampleApp.exe"));
				var obfuscatedBytes = File.ReadAllBytes(obfuscatedExe);
				Assert.NotEqual(originalBytes, obfuscatedBytes);

				// Assert — obfuscated exe runs and produces correct output
				var runResult = RunProcess(obfuscatedExe, "");
				output.WriteLine("=== Obfuscated App Output ===");
				output.WriteLine(runResult.stdout);
				Assert.Equal(42, runResult.exitCode);
				Assert.Contains("START", runResult.stdout);
				Assert.Contains("Hello from SampleApp", runResult.stdout);
				Assert.Contains("END", runResult.stdout);
			}
			finally {
				try { Directory.Delete(testDir, true); } catch { }
			}
		}

		[Fact]
		public void Cli_NoArgs_ReturnsNonZeroAndShowsUsage() {
			var cliDll = Path.Combine(AppContext.BaseDirectory, "Confuser.CLI.dll");
			var result = RunProcess("dotnet", $"\"{cliDll}\" -n");
			output.WriteLine(result.stdout);
			Assert.NotEqual(0, result.exitCode);
			Assert.Contains("Usage", result.stdout);
		}

		static (string stdout, string stderr, int exitCode) RunProcess(string fileName, string arguments) {
			var psi = new ProcessStartInfo {
				FileName = fileName,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = Process.Start(psi);
			var stdout = process.StandardOutput.ReadToEnd();
			var stderr = process.StandardError.ReadToEnd();
			process.WaitForExit(120_000);
			return (stdout, stderr, process.ExitCode);
		}
	}
}
