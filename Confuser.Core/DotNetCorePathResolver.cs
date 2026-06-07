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
		public static IEnumerable<string> ResolveRuntimePaths(string modulePath) {
			var paths = new List<string>();

			// 1. Try runtimeconfig.json next to the module
			var runtimeConfig = FindRuntimeConfig(modulePath);
			if (runtimeConfig != null)
				paths.AddRange(GetPathsFromRuntimeConfig(runtimeConfig));

			// 2. Fallback: probe standard dotnet locations
			if (paths.Count == 0)
				paths.AddRange(ProbeStandardPaths());

			return paths.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
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

		static IEnumerable<string> GetPathsFromRuntimeConfig(string configPath) {
			string content;
			try { content = File.ReadAllText(configPath); }
			catch { yield break; }

			var frameworks = ParseFrameworks(content);
			var dotnetRoot = GetDotNetRoot();
			if (dotnetRoot == null)
				yield break;

			foreach (var fw in frameworks) {
				var sharedDir = Path.Combine(dotnetRoot, "shared", fw.Name);
				if (!Directory.Exists(sharedDir))
					continue;

				// Try exact version first
				var exactPath = Path.Combine(sharedDir, fw.Version);
				if (Directory.Exists(exactPath)) {
					yield return exactPath;
					continue;
				}

				// Try latest matching major.minor
				var majorMinor = GetMajorMinor(fw.Version);
				var best = Directory.GetDirectories(sharedDir)
					.Where(d => Path.GetFileName(d).StartsWith(majorMinor, StringComparison.Ordinal))
					.OrderByDescending(d => d)
					.FirstOrDefault();
				if (best != null)
					yield return best;
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

		static IEnumerable<string> ProbeStandardPaths() {
			var dotnetRoot = GetDotNetRoot();
			if (dotnetRoot == null)
				yield break;

			var sharedDir = Path.Combine(dotnetRoot, "shared");
			if (!Directory.Exists(sharedDir))
				yield break;

			foreach (var frameworkDir in Directory.GetDirectories(sharedDir)) {
				var latest = Directory.GetDirectories(frameworkDir)
					.OrderByDescending(d => d)
					.FirstOrDefault();
				if (latest != null)
					yield return latest;
			}
		}

		struct FrameworkRef {
			public string Name;
			public string Version;
		}
	}
}
