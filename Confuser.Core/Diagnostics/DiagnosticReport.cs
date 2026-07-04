using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Confuser.Core.Project;
using dnlib.DotNet;
using Microsoft.Extensions.Logging;

namespace Confuser.Core.Diagnostics {
	/// <summary>
	///     Formats the data captured by a <see cref="DiagnosticCollector" /> into a self-contained
	///     markdown report suitable for pasting into a bug report.
	/// </summary>
	public static class DiagnosticReport {
		/// <summary>
		///     Generates the report for the given collector. Never throws — on any failure it returns
		///     a minimal report noting the failure, because this runs precisely when things are already
		///     going wrong.
		/// </summary>
		public static string Generate(DiagnosticCollector collector) {
			if (collector == null)
				return string.Empty;

			try {
				return Build(collector);
			}
			catch (Exception ex) {
				return "# ConfuserEx Diagnostic Report" + Environment.NewLine + Environment.NewLine +
					"Report generation failed: " + ex.Message + Environment.NewLine;
			}
		}

		static string Build(DiagnosticCollector collector) {
			string userProfile = SafeUserProfile();
			var sb = new StringBuilder();
			sb.AppendLine("# ConfuserEx Diagnostic Report");
			sb.AppendLine();
			AppendSystem(sb);
			AppendProject(sb, collector.Project, userProfile);
			AppendResult(sb, collector.Successful);
			AppendLog(sb, collector, userProfile);
			AppendElapsed(sb, collector.Elapsed);
			return sb.ToString();
		}

		static void AppendSystem(StringBuilder sb) {
			sb.AppendLine("## System");
			sb.AppendLine("- OS: " + SafeOsDescription());
			sb.AppendLine("- Runtime: " + SafeRuntimeDescription());
			sb.AppendLine("- Architecture: " + RuntimeInformation.OSArchitecture +
				" (process " + RuntimeInformation.ProcessArchitecture + ")");
			sb.AppendLine("- ConfuserExx: " + ConfuserEngine.Version);
			sb.AppendLine();
		}

		static void AppendProject(StringBuilder sb, ConfuserProject project, string userProfile) {
			sb.AppendLine("## Project Configuration");
			if (project == null) {
				sb.AppendLine("- (no project information available)");
				sb.AppendLine();
				return;
			}

			sb.AppendLine("- Base Directory: " + Show(DiagnosticRedactor.Redact(project.BaseDirectory, userProfile)));
			sb.AppendLine("- Output Directory: " + Show(DiagnosticRedactor.Redact(project.OutputDirectory, userProfile)));

			var modules = project.Where(m => !m.IsExternal).Select(m => m.Path)
				.Where(p => !string.IsNullOrEmpty(p)).ToList();
			sb.AppendLine("- Modules: " + (modules.Count > 0 ? string.Join(", ", modules) : "(none)"));

			var targetFrameworks = modules
				.Select(m => TryReadTargetFramework(ResolveModulePath(project, m)))
				.Where(tfm => !string.IsNullOrEmpty(tfm))
				.Distinct()
				.ToList();
			if (targetFrameworks.Count > 0)
				sb.AppendLine("- Target Framework: " + string.Join(", ", targetFrameworks));

			var externals = project.Where(m => m.IsExternal).Select(m => m.Path)
				.Where(p => !string.IsNullOrEmpty(p)).ToList();
			if (externals.Count > 0)
				sb.AppendLine("- External Modules: " + string.Join(", ", externals));

			var protections = project.Rules
				.SelectMany(r => r)
				.Select(s => s.Id)
				.Where(id => !string.IsNullOrEmpty(id))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			sb.AppendLine("- Protections: " + (protections.Count > 0 ? string.Join(", ", protections) : "(preset only / none)"));

			var presets = project.Rules.Where(r => r.Preset != ProtectionPreset.None)
				.Select(r => r.Preset.ToString().ToLowerInvariant())
				.Distinct().ToList();
			if (presets.Count > 0)
				sb.AppendLine("- Presets: " + string.Join(", ", presets));

			sb.AppendLine("- Packer: " + (project.Packer != null && !string.IsNullOrEmpty(project.Packer.Id)
				? project.Packer.Id : "(none)"));

			var probePaths = (project.ProbePaths ?? Enumerable.Empty<string>())
				.Select(p => DiagnosticRedactor.Redact(p, userProfile)).ToList();
			sb.AppendLine("- Probe Paths: " + (probePaths.Count > 0 ? string.Join(", ", probePaths) : "(none)"));

			var pluginPaths = (project.PluginPaths ?? Enumerable.Empty<string>())
				.Select(p => DiagnosticRedactor.Redact(p, userProfile)).ToList();
			if (pluginPaths.Count > 0)
				sb.AppendLine("- Plugins: " + string.Join(", ", pluginPaths));

			sb.AppendLine();
		}

		static void AppendResult(StringBuilder sb, bool? successful) {
			string status = successful == true ? "SUCCESS" : successful == false ? "FAILED" : "(did not complete)";
			sb.AppendLine("## Result: " + status);
			sb.AppendLine();
		}

		static void AppendLog(StringBuilder sb, DiagnosticCollector collector, string userProfile) {
			sb.AppendLine("## Log Output");

			var lines = new List<string>();
			if (collector.DroppedCount > 0)
				lines.Add("... " + collector.DroppedCount + " earlier log entries truncated ...");

			foreach (var entry in collector.Snapshot()) {
				lines.Add(Prefix(entry.Level) + DiagnosticRedactor.Redact(entry.Message, userProfile));
				if (!string.IsNullOrEmpty(entry.Exception))
					foreach (var exLine in entry.Exception.Split('\n'))
						lines.Add(DiagnosticRedactor.Redact(exLine.TrimEnd('\r'), userProfile));
			}

			string body = string.Join(Environment.NewLine, lines);
			string fence = MakeFence(body);
			sb.AppendLine(fence);
			sb.AppendLine(body);
			sb.AppendLine(fence);
			sb.AppendLine();
		}

		static void AppendElapsed(StringBuilder sb, TimeSpan elapsed) {
			sb.AppendLine("## Elapsed: " +
				elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s");
		}

		/// <summary>
		///     Chooses a code-fence longer than the longest run of backticks in <paramref name="body" />,
		///     so log content containing its own <c>```</c> fences cannot break out of the block.
		/// </summary>
		static string MakeFence(string body) {
			int max = 0, run = 0;
			foreach (char c in body) {
				if (c == '`') {
					run++;
					if (run > max) max = run;
				}
				else {
					run = 0;
				}
			}

			return new string('`', Math.Max(3, max + 1));
		}

		static string Prefix(LogLevel level) {
			switch (level) {
				case LogLevel.Trace:
				case LogLevel.Debug:
					return "[DEBUG] ";
				case LogLevel.Information:
					return "[INFO] ";
				case LogLevel.Warning:
					return "[WARN] ";
				case LogLevel.Error:
				case LogLevel.Critical:
					return "[ERROR] ";
				default:
					return "";
			}
		}

		static string Show(string value) => string.IsNullOrEmpty(value) ? "(not set)" : value;

		static string ResolveModulePath(ConfuserProject project, string modulePath) {
			try {
				if (!string.IsNullOrEmpty(project.BaseDirectory))
					return Path.Combine(project.BaseDirectory, modulePath);
			}
			catch {
				// Fall through to the bare module path.
			}

			return modulePath;
		}

		/// <summary>
		///     Best-effort read of an assembly's target-framework moniker (e.g.
		///     <c>.NETCoreApp,Version=v8.0</c>) from its <c>TargetFrameworkAttribute</c>. Returns
		///     <c>null</c> if the file is missing, is not a valid assembly, or carries no such
		///     attribute. The file is read into memory so it is never locked.
		/// </summary>
		public static string TryReadTargetFramework(string assemblyPath) {
			try {
				if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
					return null;

				using (var module = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath))) {
					var assembly = module.Assembly;
					if (assembly == null)
						return null;

					foreach (var attr in assembly.CustomAttributes) {
						if (attr.TypeFullName != "System.Runtime.Versioning.TargetFrameworkAttribute")
							continue;
						if (attr.ConstructorArguments.Count == 0)
							continue;

						var moniker = attr.ConstructorArguments[0].Value?.ToString();
						if (!string.IsNullOrEmpty(moniker))
							return moniker;
					}
				}
			}
			catch {
				// Diagnostic best-effort: any failure to read the framework is non-fatal.
			}

			return null;
		}

		static string SafeOsDescription() {
			try {
				return RuntimeInformation.OSDescription.Trim();
			}
			catch {
				return Environment.OSVersion.ToString();
			}
		}

		static string SafeRuntimeDescription() {
			try {
				return RuntimeInformation.FrameworkDescription;
			}
			catch {
				return ".NET " + Environment.Version;
			}
		}

		static string SafeUserProfile() {
			try {
				return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			}
			catch {
				return null;
			}
		}
	}
}
