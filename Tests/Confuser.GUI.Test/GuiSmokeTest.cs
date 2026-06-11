using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Xunit;
using Xunit.Abstractions;

namespace Confuser.GUI.Test {
	public class GuiSmokeTest : IDisposable {
		readonly ITestOutputHelper output;
		Application app;
		UIA3Automation automation;

		public GuiSmokeTest(ITestOutputHelper output) {
			this.output = output;
			automation = new UIA3Automation();
		}

		public void Dispose() {
			try { app?.Close(); } catch { }
			try { app?.Dispose(); } catch { }
			automation?.Dispose();
		}

		static string FindGuiExe() {
			// Walk up from test bin to solution root, then into ConfuserEx output
			var dir = AppContext.BaseDirectory;
			for (int i = 0; i < 6; i++) {
				var candidate = Path.Combine(dir, "ConfuserEx", "bin", "Release", "net10.0-windows", "ConfuserEx.exe");
				if (File.Exists(candidate)) return candidate;
				dir = Path.GetDirectoryName(dir);
				if (dir == null) break;
			}

			// Fallback: search relative to solution
			var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
			var fallback = Path.Combine(solutionDir, "ConfuserEx", "bin", "Release", "net10.0-windows", "ConfuserEx.exe");
			return fallback;
		}

		Application LaunchGui(string arguments = null) {
			var exePath = FindGuiExe();
			output.WriteLine($"GUI exe: {exePath}");
			Assert.True(File.Exists(exePath), $"ConfuserEx.exe not found at {exePath}. Build the solution first.");

			var psi = new ProcessStartInfo(exePath) { UseShellExecute = false };
			if (arguments != null) psi.Arguments = arguments;

			app = Application.Launch(psi);
			return app;
		}

		Window WaitForMainWindow(Application application, int timeoutSeconds = 15) {
			var mainWindow = Retry.WhileNull(
				() => application.GetMainWindow(automation),
				TimeSpan.FromSeconds(timeoutSeconds),
				TimeSpan.FromMilliseconds(500)).Result;

			Assert.NotNull(mainWindow);
			output.WriteLine($"Main window: '{mainWindow.Title}'");
			return mainWindow;
		}

		[Fact]
		public void Gui_Launches_ShowsMainWindow() {
			// Act — launch the GUI
			LaunchGui();
			var mainWindow = WaitForMainWindow(app);

			// Assert — window title contains project name and version
			Assert.Contains("Unnamed.crproj", mainWindow.Title);
			Assert.Contains("Confuser", mainWindow.Title);

			// Verify the tab control exists with expected tabs
			var tabs = mainWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.TabItem));
			output.WriteLine($"Found {tabs.Length} tabs");
			Assert.True(tabs.Length >= 4, "Expected at least 4 tabs (Project, Settings, Protect!, About)");
		}

		[Fact]
		public void Gui_LoadProject_ShowsModules() {
			// Arrange — create a .crproj that references the SampleApp
			var testDir = Path.Combine(Path.GetTempPath(), "confuserex-gui-test-" + Guid.NewGuid().ToString("N")[..8]);
			Directory.CreateDirectory(testDir);

			try {
				var sampleAppExe = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.exe");
				Assert.True(File.Exists(sampleAppExe), $"SampleApp.exe not found at {sampleAppExe}");

				// Copy sample app to test dir
				File.Copy(sampleAppExe, Path.Combine(testDir, "SampleApp.exe"));

				// Create .crproj
				var crprojPath = Path.Combine(testDir, "Test.crproj");
				File.WriteAllText(crprojPath,
@"<project outputDir="".\obfuscated"" baseDir=""."" xmlns=""http://confuser.codeplex.com"">
  <module path=""SampleApp.exe"" />
</project>");

				// Act — launch GUI with the project file
				LaunchGui($"\"{crprojPath}\"");
				var mainWindow = WaitForMainWindow(app);

				// Assert — title shows the project file name
				Assert.Contains("Test.crproj", mainWindow.Title);

				// Find the modules list and verify SampleApp.exe appears
				var found = Retry.WhileEmpty(
					() => mainWindow.FindAllDescendants(cf => cf.ByText("SampleApp.exe")),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromMilliseconds(500)).Result;

				output.WriteLine($"Found {found.Length} elements with 'SampleApp.exe'");
				Assert.True(found.Length > 0, "SampleApp.exe should appear in the modules list");
			}
			finally {
				try { Directory.Delete(testDir, true); } catch { }
			}
		}

		[Fact]
		public void Gui_ProtectSampleApp_ShowsSuccess() {
			// Arrange — create a project with rename protection
			var testDir = Path.Combine(Path.GetTempPath(), "confuserex-gui-protect-" + Guid.NewGuid().ToString("N")[..8]);
			Directory.CreateDirectory(testDir);
			var outputDir = Path.Combine(testDir, "obfuscated");

			try {
				// Copy SampleApp + Confuser.Runtime to test dir
				var sampleAppExe = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.exe");
				Assert.True(File.Exists(sampleAppExe), $"SampleApp.exe not found at {sampleAppExe}");
				File.Copy(sampleAppExe, Path.Combine(testDir, "SampleApp.exe"));

				// Create .crproj with rename protection
				var crprojPath = Path.Combine(testDir, "Test.crproj");
				File.WriteAllText(crprojPath,
@"<project outputDir="".\obfuscated"" baseDir=""."" xmlns=""http://confuser.codeplex.com"">
  <rule pattern=""true"" preset=""none"" inherit=""false"">
    <protection id=""rename"" />
  </rule>
  <module path=""SampleApp.exe"" />
</project>");

				// Act — launch GUI with the project
				LaunchGui($"\"{crprojPath}\"");
				var mainWindow = WaitForMainWindow(app);

				// Navigate to the Protect! tab
				var protectTab = Retry.WhileNull(
					() => mainWindow.FindFirstDescendant(cf => cf.ByText("Protect!")),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromMilliseconds(500)).Result;
				Assert.NotNull(protectTab);
				protectTab.Click();

				// Find and click the Protect! button
				var protectButton = Retry.WhileNull(
					() => {
						var buttons = mainWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));
						foreach (var btn in buttons) {
							if (btn.Name == "Protect!") return btn;
						}
						return null;
					},
					TimeSpan.FromSeconds(5),
					TimeSpan.FromMilliseconds(500)).Result;

				Assert.NotNull(protectButton);
				output.WriteLine("Clicking Protect! button...");
				protectButton.Click();

				// Wait for "Finished" to appear in the log (indicates success)
				var finishedText = Retry.WhileNull(
					() => {
						var richTextBoxes = mainWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Document));
						foreach (var rtb in richTextBoxes) {
							var text = rtb.Name ?? "";
							if (text.Contains("Finished")) return rtb;
						}
						return null;
					},
					TimeSpan.FromSeconds(30),
					TimeSpan.FromMilliseconds(1000)).Result;

				// If direct text check didn't work, wait for the output file to appear
				if (finishedText == null) {
					Retry.WhileNull(
						() => File.Exists(Path.Combine(outputDir, "SampleApp.exe")) ? (object)true : null,
						TimeSpan.FromSeconds(30),
						TimeSpan.FromMilliseconds(1000));
				}

				output.WriteLine($"Protection completed. Output exists: {File.Exists(Path.Combine(outputDir, "SampleApp.exe"))}");
				Assert.True(File.Exists(Path.Combine(outputDir, "SampleApp.exe")),
					"Obfuscated SampleApp.exe should exist after protection");

				// Verify the obfuscated exe runs correctly
				var runResult = RunProcess(Path.Combine(outputDir, "SampleApp.exe"), "");
				output.WriteLine($"Obfuscated output: {runResult.stdout}");
				Assert.Equal(42, runResult.exitCode);
				Assert.Contains("Hello from SampleApp", runResult.stdout);
			}
			finally {
				try { Directory.Delete(testDir, true); } catch { }
			}
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
			process.WaitForExit(60_000);
			return (stdout, stderr, process.ExitCode);
		}
	}
}
