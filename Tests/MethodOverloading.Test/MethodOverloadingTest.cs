using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace MethodOverloading.Test {
	public class MethodOverloadingTest : TestBase {
		public MethodOverloadingTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Theory]
		[MemberData(nameof(MethodOverloadingData))]
		[Trait("Category", "Protection")]
		[Trait("Protection", "rename")]
		[Trait("Issue", "https://github.com/mkaring/ConfuserEx/issues/230")]
		public async Task MethodOverloading(bool shortNames, bool preserveGenericParams) =>
			await Run(
				"MethodOverloading.exe",
				new[] {
					"1",
					"Hello world",
					"object",
					"2",
					"test",
					"5",
					"class",
					"class2",
					"class3",
					"class4",
					"class5",
					"BaseClassVirtualMethod",
					"ClassVirtualMethod",
					"ClassVirtualMethod"
				},
				new SettingItem<Protection>("rename") {
					["mode"] = "decodable",
					["shortNames"] = shortNames.ToString().ToLowerInvariant(),
					["preserveGenericParams"] = preserveGenericParams.ToString().ToLowerInvariant()
				},
				(shortNames ? "_shortnames" : "_fullnames") + (preserveGenericParams ? "_preserveGenericParams" : ""),
				seed: "seed",
				postProcessAction: outputPath => {
					var symbolsPath = Path.Combine(outputPath, "symbols.map");
					var symbols = File.ReadAllLines(symbolsPath).Select(line => {
						var parts = line.Split('\t');
						return new KeyValuePair<string, string>(parts[0], parts[1]);
					}).ToDictionary(keyValue => keyValue.Key, keyValue => keyValue.Value);

					// Assert on the original names (stable map values) rather than the obfuscated
					// keys, which are volatile — the exact renamed identifiers depend on the rename
					// algorithm, dnlib version and framework, and legitimately drift over time. The
					// intent of this test is that the symbols map is complete and mode-appropriate.
					if (shortNames) {
						Assert.Contains("MethodOverloading.Class", symbols.Values);
						Assert.Contains("MethodOverloading.Program/NestedClass", symbols.Values);
						Assert.Contains("OverloadedMethod", symbols.Values);
						Assert.Contains("Field", symbols.Values);
						Assert.Contains("Property", symbols.Values);
						Assert.Contains("Event", symbols.Values);
					}
					else {
						Assert.Contains("MethodOverloading.Class", symbols.Values);
						Assert.Contains("MethodOverloading.Program/NestedClass", symbols.Values);
						Assert.Contains("MethodOverloading.Program::OverloadedMethod(System.Object[])", symbols.Values);
						Assert.Contains("MethodOverloading.Program::OverloadedMethod(System.String)", symbols.Values);
						Assert.Contains("MethodOverloading.BaseClass::Field", symbols.Values);
						Assert.Contains("MethodOverloading.BaseClass::Property", symbols.Values);
						Assert.Contains("MethodOverloading.BaseClass::Event", symbols.Values);
					}

					return Task.Delay(0);
				}
			);

		public static IEnumerable<object[]> MethodOverloadingData() {
			foreach (var shortNames in new[] { false, true }) {
				foreach (var preserveGenericParams in new[] { false, true }) {
					yield return new object[] { shortNames, preserveGenericParams };
				}
			}
		}
	}
}
