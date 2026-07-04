using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace SymbolMapReuse.Test {
	public sealed class SymbolMapReuseTest {
		readonly ITestOutputHelper output;

		public SymbolMapReuseTest(ITestOutputHelper output) => this.output = output;

		// Obfuscation names are deterministic per seed, so two runs with different seeds normally
		// produce different names. With InputSymbolMap set to a previous run's symbols.map, the
		// renamer must reuse those names — so re-obfuscation stays consistent across builds even
		// when the seed (or code) changes. This is the core guarantee of issue #26.
		[Fact]
		[Trait("Category", "Renamer")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/26")]
		public async Task InputSymbolMap_ReusesNames_AcrossDifferentSeeds() {
			var baseDir = Environment.CurrentDirectory;
			const string subject = "AntiTamper.exe";
			Assert.True(File.Exists(Path.Combine(baseDir, subject)), $"Test subject {subject} not found in {baseDir}");

			// Run 1 — fresh names with seed A.
			var map1 = await ObfuscateAndReadMap(baseDir, subject, seed: "SeedAAAAAAAA", inputMap: null, suffix: "-map-run1");
			Assert.NotEmpty(map1);

			// Run 2 — DIFFERENT seed, but feed run 1's map back in: names must be reused.
			var run1MapPath = Path.Combine(baseDir, "obf-map-run1", "symbols.map");
			var map2 = await ObfuscateAndReadMap(baseDir, subject, seed: "SeedBBBBBBBB", inputMap: run1MapPath, suffix: "-map-run2");
			Assert.Equal(map1, map2);

			// Control — different seed, NO input map: names must differ (proving reuse caused the match).
			var map3 = await ObfuscateAndReadMap(baseDir, subject, seed: "SeedBBBBBBBB", inputMap: null, suffix: "-map-run3");
			Assert.NotEqual(map1, map3);
		}

		async Task<List<string>> ObfuscateAndReadMap(string baseDir, string subject, string seed, string inputMap, string suffix) {
			var outputDir = Path.Combine(baseDir, "obf" + suffix);
			if (Directory.Exists(outputDir))
				Directory.Delete(outputDir, true);

			var proj = new ConfuserProject {
				BaseDirectory = baseDir,
				OutputDirectory = outputDir,
				Seed = seed,
				InputSymbolMap = inputMap
			};
			proj.Add(new ProjectModule { Path = subject });

			var rule = new Rule();
			// Decodable mode records every rename in symbols.map, giving a stable map to compare.
			rule.Add(new SettingItem<Protection>("rename") { { "mode", "decodable" } });
			proj.Rules.Add(rule);

			var logger = new XunitLogger(output);
			await ConfuserEngine.Run(new ConfuserParameters {
				Project = proj,
				Logger = logger,
				ProgressReporter = logger
			});

			var mapPath = Path.Combine(outputDir, "symbols.map");
			Assert.True(File.Exists(mapPath), $"symbols.map should be produced in {outputDir}");

			// Return the mapping lines sorted, so comparison is order-insensitive.
			return File.ReadAllLines(mapPath)
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.OrderBy(line => line, StringComparer.Ordinal)
				.ToList();
		}
	}
}
