using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Confuser.UnitTest {
	public abstract class TestBase {
		private const string _externalPrefix = "external:";

		readonly ITestOutputHelper outputHelper;

		protected static IEnumerable<SettingItem<Protection>> NoProtections => Enumerable.Empty<SettingItem<Protection>>();

		protected TestBase(ITestOutputHelper outputHelper) =>
			this.outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));

		protected Task Run(string inputFileName, string[] expectedOutput, SettingItem<Protection> protection,
			string outputDirSuffix = "", Action<string> outputAction = null, SettingItem<Packer> packer = null,
			Action<ProjectModule> projectModuleAction = null, Func<string, Task> postProcessAction = null,
			string seed = null, bool checkOutput = true, string processArguments = null, bool runEntryProcess = true) =>

			Run(new[] { inputFileName }, expectedOutput, protection, outputDirSuffix, outputAction, packer,
				projectModuleAction, postProcessAction, seed, checkOutput, processArguments, runEntryProcess);

		protected Task Run(string inputFileName, string[] expectedOutput, IEnumerable<SettingItem<Protection>> protections,
			string outputDirSuffix = "", Action<string> outputAction = null, SettingItem<Packer> packer = null,
			Action<ProjectModule> projectModuleAction = null, Func<string, Task> postProcessAction = null,
			string processArguments = null) =>

			Run(new[] { inputFileName }, expectedOutput, protections, outputDirSuffix, outputAction, packer,
				projectModuleAction, postProcessAction, processArguments: processArguments);

		protected Task Run(string[] inputFileNames, string[] expectedOutput, SettingItem<Protection> protection,
			string outputDirSuffix = "", Action<string> outputAction = null, SettingItem<Packer> packer = null,
			Action<ProjectModule> projectModuleAction = null, Func<string, Task> postProcessAction = null,
			string seed = null, bool checkOutput = true, string processArguments = null, bool runEntryProcess = true) {
			var protections = (protection is null) ? Enumerable.Empty<SettingItem<Protection>>() : new[] { protection };
			return Run(inputFileNames, expectedOutput, protections, outputDirSuffix, outputAction, packer, projectModuleAction, postProcessAction, seed, checkOutput, processArguments, runEntryProcess);
		}

		protected async Task Run(string[] inputFileNames, string[] expectedOutput, IEnumerable<SettingItem<Protection>> protections,
			string outputDirSuffix = "", Action<string> outputAction = null, SettingItem<Packer> packer = null,
			Action<ProjectModule> projectModuleAction = null, Func<string, Task> postProcessAction = null,
			string seed = null, bool checkOutput = true, string processArguments = null, bool runEntryProcess = true) {

			var baseDir = Environment.CurrentDirectory;
			var outputDir = Path.Combine(baseDir, "obfuscated" + outputDirSuffix);
			if (Directory.Exists(outputDir)) {
				Directory.Delete(outputDir, true);
			}

			string firstFileName = GetFileName(inputFileNames[0]);
			string entryInputFileName = Path.Combine(baseDir, firstFileName);
			var entryOutputFileName = Path.Combine(outputDir, firstFileName);
			var proj = new ConfuserProject {
				BaseDirectory = baseDir,
				OutputDirectory = outputDir,
				Packer = packer,
				Seed = seed
			};

			foreach (string name in inputFileNames) {
				if (IsExternal(name) && !IsAssembly(name))
					continue;
				var projectModule = new ProjectModule {
					Path = Path.Combine(baseDir, GetFileName(name)),
					IsExternal = IsExternal(name)
				};
				projectModuleAction?.Invoke(projectModule);
				proj.Add(projectModule);
			}

			var rule = new Rule();
			rule.AddRange(protections);
			if (rule.Count > 0)
				proj.Rules.Add(rule);

			var xunitLogger = new XunitLogger(outputHelper, outputAction);
			var parameters = new ConfuserParameters {
				Project = proj,
				Logger = xunitLogger,
				ProgressReporter = xunitLogger
			};

			await ConfuserEngine.Run(parameters);

			for (var index = 0; index < inputFileNames.Length; index++) {
				string name = GetFileName(inputFileNames[index]);
				string outputName = Path.Combine(outputDir, name);

				bool exists;
				if (index == 0) {
					Assert.True(File.Exists(outputName));
					exists = true;
				}
				else {
					exists = File.Exists(outputName);
				}

				if (exists && IsAssembly(inputFileNames[index])) {
					// Check if output assemblies is obfuscated. Non-assembly support files
					// (e.g. runtimeconfig.json copied next to the module) are verbatim copies.
					Assert.NotEqual(FileUtilities.ComputeFileChecksum(Path.Combine(baseDir, name)),
						FileUtilities.ComputeFileChecksum(outputName));
				}
				else if (!exists && IsExternal(inputFileNames[index])) {
					File.Copy(
						Path.Combine(baseDir, GetFileName(inputFileNames[index])),
						Path.Combine(outputDir, GetFileName(inputFileNames[index])));
				}
			}

			if (runEntryProcess && Path.GetExtension(entryInputFileName) == ".exe") {
				var info = new ProcessStartInfo(entryOutputFileName) {
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					Arguments = processArguments ?? ""
				};
				using (var process = Process.Start(info)) {
					using (var stdout = process.StandardOutput) {
						try {
							if (checkOutput) {
								Assert.Equal("START", await stdout.ReadLineAsync());

								foreach (string line in expectedOutput) {
									Assert.Equal(line, await stdout.ReadLineAsync());
								}

								Assert.Equal("END", await stdout.ReadLineAsync());
								Assert.Empty(await stdout.ReadToEndAsync());
							}
						}
						catch (XunitException) {
							try {
								LogRemainingStream("Remaining standard output:", stdout);
								using (var stderr = process.StandardError) {
									LogRemainingStream("Remaining standard error:", stderr);
								}
							}
							catch {
								// ignore
							}
							throw;
						}
					}

					using (var stderr = process.StandardError) {
						Assert.Empty(await stderr.ReadToEndAsync());
					}

					Assert.True(process.HasExited);
					Assert.Equal(42, process.ExitCode);
				}
			}

			if (!(postProcessAction is null))
				await postProcessAction.Invoke(outputDir);
		}

		private void LogRemainingStream(string header, StreamReader reader) {
			var remainingOutput = reader.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(remainingOutput)) {
				outputHelper.WriteLine(header);
				outputHelper.WriteLine(remainingOutput.Trim());
			}
		}

		private static string GetFileName(string name) {
			if (IsExternal(name))
				return name.Substring(_externalPrefix.Length);
			return name;
		}

		private static bool IsExternal(string name) => name.StartsWith(_externalPrefix, StringComparison.OrdinalIgnoreCase);

		private static bool IsAssembly(string name) {
			string extension = Path.GetExtension(GetFileName(name));
			return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
				extension.Equals(".exe", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		///     Runs an obfuscated application from its output directory and returns the exit code
		///     together with the captured output streams. A <c>.exe</c> (.NET Framework) is launched
		///     directly; a <c>.dll</c> (.NET Core) is launched via <c>dotnet &lt;dll&gt;</c>.
		/// </summary>
		protected static (int ExitCode, string StdOut, string StdErr) RunApp(
			string workingDirectory, string appFileName, string arguments = null) {
			ProcessStartInfo startInfo;
			if (appFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
				startInfo = new ProcessStartInfo(Path.Combine(workingDirectory, appFileName)) {
					Arguments = arguments ?? "",
					WorkingDirectory = workingDirectory
				};
			}
			else {
				startInfo = new ProcessStartInfo("dotnet",
					string.IsNullOrEmpty(arguments) ? appFileName : appFileName + " " + arguments) {
					WorkingDirectory = workingDirectory
				};
			}
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;

			using var process = Process.Start(startInfo);
			string stdout = process.StandardOutput.ReadToEnd();
			string stderr = process.StandardError.ReadToEnd();
			process.WaitForExit();
			return (process.ExitCode, stdout, stderr);
		}

		/// <summary>
		///     Obfuscates the given UI application, then launches the obfuscated build with
		///     <c>--selftest</c>: it shows its real window off-screen, pumps its message loop and
		///     self-closes. Asserts the app actually started and exited cleanly (42) without crashing.
		/// </summary>
		protected Task SelfTestRunsWithoutCrashing(string appFile, string outputDirSuffix) =>
			Run(appFile, null, new SettingItem<Protection>("rename"),
				outputDirSuffix: outputDirSuffix, checkOutput: false, runEntryProcess: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunApp(outputPath, appFile, "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("SHOWN:", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});

		/// <summary>
		///     Obfuscates the given UI application, then launches it with <c>--selftest-crash</c>,
		///     which throws once the window is shown. Asserts the app's unhandled-exception handler
		///     caught the crash and reported failure (non-42 exit, <c>CRASH</c> on stdout).
		/// </summary>
		/// <summary>
		///     Obfuscates the given console application, then runs the obfuscated build with
		///     <c>--selftest</c> and asserts it executed to completion and exited cleanly (42).
		/// </summary>
		protected Task SelfTestConsoleRuns(string appFile, string outputDirSuffix) =>
			Run(appFile, null, new SettingItem<Protection>("rename"),
				outputDirSuffix: outputDirSuffix, checkOutput: false, runEntryProcess: false,
				postProcessAction: outputPath => {
					var (exit, stdout, stderr) = RunApp(outputPath, appFile, "--selftest");
					Assert.Equal(42, exit);
					Assert.Contains("RUN", stdout);
					Assert.DoesNotContain("CRASH", stdout);
					Assert.Empty(stderr);
					return Task.CompletedTask;
				});

		protected Task SelfTestReportsCrash(string appFile, string outputDirSuffix) =>
			Run(appFile, null, new SettingItem<Protection>("rename"),
				outputDirSuffix: outputDirSuffix, checkOutput: false, runEntryProcess: false,
				postProcessAction: outputPath => {
					var (exit, stdout, _) = RunApp(outputPath, appFile, "--selftest-crash");
					Assert.NotEqual(42, exit);
					Assert.Contains("CRASH", stdout);
					return Task.CompletedTask;
				});
	}
}
