using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Confuser.Core {
	/// <summary>
	///     Discovers .NET Core/5+/6/7/8+ runtime assembly directories for assembly resolution.
	/// </summary>
	internal static class DotNetCorePathResolver {
		/// <summary>
		///     Resolves runtime assembly paths for a given module by parsing its runtimeconfig.json
		///     or probing standard dotnet installation directories.
		/// </summary>
		public static IEnumerable<string> ResolveRuntimePaths(string modulePath, ILogger logger) {
			// 1. Try runtimeconfig.json next to the module (target framework gets priority)
			var runtimeConfigPaths = Enumerable.Empty<string>();
			var runtimeConfig = FindRuntimeConfig(modulePath);
			if (runtimeConfig != null)
				runtimeConfigPaths = GetPathsFromRuntimeConfig(runtimeConfig, logger);

			// 2. Always add ALL installed runtime versions to handle cross-version
			//    dependencies (e.g., net10.0 app referencing a library compiled against net8.0)
			return runtimeConfigPaths
				.Concat(ProbeAllInstalledRuntimes(logger))
				.Where(Directory.Exists)
				.Distinct(StringComparer.OrdinalIgnoreCase);
		}

		static string FindRuntimeConfig(string modulePath) {
			if (string.IsNullOrEmpty(modulePath))
				return null;
			var dir = Path.GetDirectoryName(modulePath);
			if (dir == null)
				return null;
			var name = Path.GetFileNameWithoutExtension(modulePath);
			var configPath = Path.Combine(dir, name + ".runtimeconfig.json");
			return File.Exists(configPath) ? configPath : null;
		}

		static IEnumerable<string> GetPathsFromRuntimeConfig(string configPath, ILogger logger) {
			string content;
			try {
				content = File.ReadAllText(configPath);
			}
			catch (IOException ex) {
				logger.WarnFormat("Failed to read runtime config '{0}': {1}", configPath, ex.Message);
				yield break;
			}
			catch (UnauthorizedAccessException ex) {
				logger.WarnFormat("Access denied reading runtime config '{0}': {1}", configPath, ex.Message);
				yield break;
			}

			var frameworks = ParseFrameworks(content);
			if (frameworks.Count == 0) {
				logger.DebugFormat("No framework references found in '{0}'.", configPath);
				yield break;
			}

			var dotnetRoot = GetDotNetRoot();
			if (dotnetRoot == null) {
				logger.Warn("Could not locate .NET installation directory. Set DOTNET_ROOT environment variable if installed in a non-standard location.");
				yield break;
			}

			foreach (var fw in frameworks) {
				var sharedDir = Path.Combine(dotnetRoot, "shared", fw.Name);
				if (!Directory.Exists(sharedDir)) {
					logger.DebugFormat("Framework directory not found: {0}", sharedDir);
					continue;
				}

				// Try exact version first
				var exactPath = Path.Combine(sharedDir, fw.Version);
				if (Directory.Exists(exactPath)) {
					yield return exactPath;
					continue;
				}

				// Try latest matching major.minor
				var majorMinor = GetMajorMinor(fw.Version);
				var best = Directory.EnumerateDirectories(sharedDir)
					.Where(d => Path.GetFileName(d).StartsWith(majorMinor, StringComparison.Ordinal))
					.OrderByDescending(d => d)
					.FirstOrDefault();
				if (best != null)
					yield return best;
				else
					logger.WarnFormat("No installed runtime found matching {0} {1} in {2}", fw.Name, fw.Version, sharedDir);
			}
		}

		static List<FrameworkRef> ParseFrameworks(string json) {
			var results = new List<FrameworkRef>();
			// Match framework objects: "name": "...", "version": "..."
			var matches = Regex.Matches(json, @"""name""\s*:\s*""([^""]+)""\s*,\s*""version""\s*:\s*""([^""]+)""");
			foreach (Match m in matches)
				results.Add(new FrameworkRef { Name = m.Groups[1].Value, Version = m.Groups[2].Value });

			// Also try reversed order (version before name)
			if (results.Count == 0) {
				matches = Regex.Matches(json, @"""version""\s*:\s*""([^""]+)""\s*,\s*""name""\s*:\s*""([^""]+)""");
				foreach (Match m in matches)
					results.Add(new FrameworkRef { Name = m.Groups[2].Value, Version = m.Groups[1].Value });
			}

			return results;
		}

		static string GetDotNetRoot() {
			// Check DOTNET_ROOT env var first
			var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
			if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
				return root;

			// Standard locations
			if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
				var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				var path = Path.Combine(programFiles, "dotnet");
				if (Directory.Exists(path))
					return path;
			}
			else {
				if (Directory.Exists("/usr/share/dotnet"))
					return "/usr/share/dotnet";
				if (Directory.Exists("/usr/local/share/dotnet"))
					return "/usr/local/share/dotnet";
			}

			return null;
		}

		static string GetMajorMinor(string version) {
			var parts = version.Split('.');
			return parts.Length >= 2 ? parts[0] + "." + parts[1] : version;
		}

		static IEnumerable<string> ProbeAllInstalledRuntimes(ILogger logger) {
			var dotnetRoot = GetDotNetRoot();
			if (dotnetRoot == null) {
				logger.Warn("Could not locate .NET installation directory for runtime probing.");
				yield break;
			}

			var sharedDir = Path.Combine(dotnetRoot, "shared");
			if (!Directory.Exists(sharedDir)) {
				logger.WarnFormat("Shared framework directory not found: {0}", sharedDir);
				yield break;
			}

			// Return ALL installed runtime versions (newest first) across all frameworks.
			// This handles cross-version dependencies where e.g., a net10.0 app references
			// a library compiled against net8.0 which needs System.Runtime 8.0.0.0.
			foreach (var frameworkDir in Directory.EnumerateDirectories(sharedDir)) {
				foreach (var versionDir in Directory.EnumerateDirectories(frameworkDir).OrderByDescending(d => d))
					yield return versionDir;
			}
		}

		struct FrameworkRef {
			public string Name;
			public string Version;
		}
	}
}
