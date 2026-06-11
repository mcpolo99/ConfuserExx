using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Confuser.Core;
using Confuser.Core.Project;
using Microsoft.Extensions.Logging;
using NDesk.Options;
using Serilog;
using Serilog.Events;

namespace Confuser.CLI {
	internal class Program {
		static int Main(string[] args) {
			ConsoleColor original = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.White;
			string originalTitle = null;
			if (OperatingSystem.IsWindows()) {
				originalTitle = Console.Title;
				Console.Title = "ConfuserEx";
			}
			try {
				bool noPause = false;
				bool debug = false;
				bool quiet = false;
				int verbosity = 0;
				string outDir = null;
				string snKeyPath = null;
				string snKeyPass = null;
				List<string> probePaths = new List<string>();
				List<string> plugins = new List<string>();
				var p = new OptionSet {
					{
						"n|nopause", "no pause after finishing protection.",
						value => { noPause = (value != null); }
					}, {
						"o|out=", "specifies output directory.",
						value => { outDir = value; }
					}, {
						"probe=", "specifies probe directory.",
						value => { probePaths.Add(value); }
					}, {
						"plugin=", "specifies plugin path.",
						value => { plugins.Add(value); }
					}, {
						"debug", "specifies debug symbol generation.",
						value => { debug = (value != null); }
					}, {
						"snkey=", "specifies strong name key file path.",
						value => { snKeyPath = value; }
					}, {
						"snkeypass=", "specifies strong name key password.",
						value => { snKeyPass = value; }
					}, {
						"v|verbose", "increase verbosity (repeat for more: -v, -vv, -vvv).",
						value => { verbosity++; }
					}, {
						"q|quiet", "only show warnings and errors.",
						value => { quiet = (value != null); }
					}
				};

				List<string> files;
				try {
					files = p.Parse(args);
					if (files.Count == 0)
						throw new ArgumentException("No input files specified.");
				}
				catch (Exception ex) {
					Console.Write("ConfuserEx.CLI: ");
					Console.WriteLine(ex.Message);
					PrintUsage();
					return -1;
				}

				var parameters = new ConfuserParameters();

				if (files.Count == 1 && Path.GetExtension(files[0]) == ".crproj") {
					var proj = new ConfuserProject();
					try {
						var xmlDoc = new XmlDocument();
						xmlDoc.Load(files[0]);
						proj.Load(xmlDoc, Path.GetDirectoryName(Path.GetFullPath(files[0])));
						proj.OutputDirectory = Path.GetFullPath(Path.Combine(proj.BaseDirectory, proj.OutputDirectory));
					}
					catch (Exception ex) {
						WriteLineWithColor(ConsoleColor.Red, "Failed to load project:");
						WriteLineWithColor(ConsoleColor.Red, ex.ToString());
						return -1;
					}

					parameters.Project = proj;
				}
				else {
					if (string.IsNullOrEmpty(outDir)) {
						Console.WriteLine("ConfuserEx.CLI: No output directory specified.");
						PrintUsage();
						return -1;
					}

					var proj = new ConfuserProject();
					var templateModules = new List<ProjectModule>();

					if (Path.GetExtension(files[files.Count - 1]) == ".crproj") {
						LoadTemplateProject(files[files.Count - 1], proj, templateModules);
						files.RemoveAt(files.Count - 1);
					}

					// Generate a ConfuserProject for input modules
					// Assuming first file = main module
					proj.BaseDirectory = Path.GetDirectoryName(files[0]);
					if (string.IsNullOrWhiteSpace(proj.BaseDirectory)) {
						WriteLineWithColor(ConsoleColor.Red, "Failed to identify base directory for main assembly.");
						PrintUsage();
						return -1;
					}

					foreach (var input in files) {
						string modulePath = input;
						if (modulePath.StartsWith(proj.BaseDirectory, StringComparison.OrdinalIgnoreCase)) {
							modulePath = modulePath.Substring(proj.BaseDirectory.Length + 1);
						}

						if (TryMatchTemplateProject(templateModules, proj.BaseDirectory, modulePath, out var matchedModule)) {
							if (snKeyPath != null) matchedModule.SNKeyPath = snKeyPath;
							if (snKeyPass != null) matchedModule.SNKeyPassword = snKeyPass;
							proj.Add(matchedModule);
						}
						else
							proj.Add(new ProjectModule { Path = modulePath, SNKeyPath = snKeyPath, SNKeyPassword = snKeyPass });
					}

					proj.OutputDirectory = outDir;
					foreach (var path in probePaths)
						proj.ProbePaths.Add(path);
					foreach (var path in plugins)
						proj.PluginPaths.Add(path);
					proj.Debug = debug;
					parameters.Project = proj;
				}

				int retVal = RunProject(parameters, quiet, verbosity);

				if (NeedPause() && !noPause) {
					Console.WriteLine("Press any key to continue...");
					Console.ReadKey(true);
				}

				return retVal;
			}
			finally {
				Console.ForegroundColor = original;
				if (OperatingSystem.IsWindows() && originalTitle != null)
					Console.Title = originalTitle;
			}
		}

		static bool TryMatchTemplateProject(List<ProjectModule> templateModules, string baseDirectory, string modulePath, out ProjectModule matchedModule) {
			var matchedToTemplate = false;
			matchedModule = null;

			foreach (var templateModule in templateModules) {
				var templatePath = templateModule.Path;
				if (templatePath.StartsWith(@".\", StringComparison.Ordinal))
					templatePath = templatePath.Substring(2);

				if (modulePath.Equals(templatePath, StringComparison.OrdinalIgnoreCase))
					matchedToTemplate = true;

				if (modulePath.Equals(Path.Combine(baseDirectory, templatePath), StringComparison.OrdinalIgnoreCase))
					matchedToTemplate = true;

				if (matchedToTemplate)
					matchedModule = templateModule;
			}

			return matchedToTemplate;
		}

		static void LoadTemplateProject(string templatePath, ConfuserProject proj, List<ProjectModule> templateModules) {
			var templateProj = new ConfuserProject();
			var xmlDoc = new XmlDocument();
			xmlDoc.Load(templatePath);
			templateProj.Load(xmlDoc);

			foreach (var rule in templateProj.Rules)
				proj.Rules.Add(rule);

			proj.Packer = templateProj.Packer;

			foreach (string pluginPath in templateProj.PluginPaths)
				proj.PluginPaths.Add(pluginPath);

			foreach (string probePath in templateProj.ProbePaths)
				proj.ProbePaths.Add(probePath);

			foreach (var templateModule in templateProj)
				if (templateModule.IsExternal)
					proj.Add(templateModule);
				else
					templateModules.Add(templateModule);
		}

		static int RunProject(ConfuserParameters parameters, bool quiet, int verbosity) {
			var levelSwitch = quiet
				? LogEventLevel.Warning
				: verbosity >= 3 ? LogEventLevel.Verbose
				: verbosity >= 2 ? LogEventLevel.Verbose
				: verbosity >= 1 ? LogEventLevel.Debug
				: LogEventLevel.Information;

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Is(levelSwitch)
				.WriteTo.Console(
					outputTemplate: "[{Level:u4}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			using var loggerFactory = LoggerFactory.Create(builder =>
				builder.AddSerilog(dispose: false));
			var melLogger = loggerFactory.CreateLogger("ConfuserEx");

			var progressReporter = new ConsoleProgressReporter();
			parameters.Logger = melLogger;
			parameters.ProgressReporter = progressReporter;

			if (OperatingSystem.IsWindows())
				Console.Title = "ConfuserEx - Running...";
			ConfuserEngine.Run(parameters).GetAwaiter().GetResult();

			Log.CloseAndFlush();
			return progressReporter.ReturnValue;
		}

		static bool NeedPause() {
			return Debugger.IsAttached || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROMPT"));
		}

		static void PrintUsage() {
			WriteLine("Usage:");
			WriteLine("Confuser.CLI -n|noPause <project configuration>");
			WriteLine("Confuser.CLI -n|noPause -o|out=<output directory> <modules>");
			WriteLine("    -n|noPause : no pause after finishing protection.");
			WriteLine("    -o|out     : specifies output directory.");
			WriteLine("    -probe     : specifies probe directory.");
			WriteLine("    -plugin    : specifies plugin path.");
			WriteLine("    -debug     : specifies debug symbol generation.");
			WriteLine("    -snkey     : specifies strong name key file path.");
			WriteLine("    -snkeypass : specifies strong name key password.");
			WriteLine("    -v|verbose : increase verbosity (-v debug, -vv trace).");
			WriteLine("    -q|quiet   : only show warnings and errors.");
		}

		static void WriteLineWithColor(ConsoleColor color, string txt) {
			ConsoleColor original = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(txt);
			Console.ForegroundColor = original;
		}

		static void WriteLine(string txt) {
			Console.WriteLine(txt);
		}

		static void WriteLine() {
			Console.WriteLine();
		}

		class ConsoleProgressReporter : IProgressReporter {
			readonly DateTime begin;

			public ConsoleProgressReporter() {
				begin = DateTime.Now;
			}

			public int ReturnValue { get; private set; }

			public void Progress(int progress, int overall) { }

			public void EndProgress() { }

			public void Finish(bool successful) {
				DateTime now = DateTime.Now;
				string timeString = string.Format(
					"at {0}, {1}:{2:d2} elapsed.",
					now.ToShortTimeString(),
					(int)now.Subtract(begin).TotalMinutes,
					now.Subtract(begin).Seconds);
				if (successful) {
					Console.Title = "ConfuserEx - Success";
					WriteLineWithColor(ConsoleColor.Green, "Finished " + timeString);
					ReturnValue = 0;
				}
				else {
					Console.Title = "ConfuserEx - Fail";
					WriteLineWithColor(ConsoleColor.Red, "Failed " + timeString);
					ReturnValue = 1;
				}
			}
		}
	}
}
