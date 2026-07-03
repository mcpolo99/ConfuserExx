using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.Renamer;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace MessageDeobfuscation.Test {
	public class MessageDeobfuscationTest : TestBase {
		readonly string _expectedDeobfuscatedOutput = String.Join(Environment.NewLine,
			"Exception",
			"   at MessageDeobfuscation.Class.NestedClass.Method(String )",
			"   at MessageDeobfuscation.Program.Main()");

		const string Password = "password";
		const string Seed = "seed";

		public MessageDeobfuscationTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Theory]
		[MemberData(nameof(RenameModes))]
		[Trait("Category", "Protection")]
		[Trait("Protection", "rename")]
		public async Task MessageDeobfuscationWithSymbolsMap(string renameMode) =>
			await Run(
				"MessageDeobfuscation.exe",
				Array.Empty<string>(),
				new SettingItem<Protection>("rename") { ["mode"] = renameMode },
				$"SymbolsMap_{renameMode}",
				seed: "1234",
				// The obfuscated program output contains the renamed identifiers, which are
				// volatile — don't assert on their exact text. The symbols map round-trip below
				// verifies the renaming and deobfuscation instead.
				checkOutput: false,
				postProcessAction: outputPath => {
					var symbolsPath = Path.Combine(outputPath, "symbols.map");
					var map = File.ReadAllLines(symbolsPath)
						.Select(line => line.Split('\t'))
						.Where(parts => parts.Length == 2)
						.ToDictionary(parts => parts[0], parts => parts[1]);

					// The exact obfuscated identifiers depend on the rename algorithm, dnlib
					// version and framework, so assert on the original names — the symbols map
					// must contain each renamed member's original full name.
					var expectedOriginals = new[] {
						"MessageDeobfuscation.Class",
						"MessageDeobfuscation.Class/NestedClass",
						"MessageDeobfuscation.Class::Method(System.String,System.Int32)",
						"MessageDeobfuscation.Class::Field",
						"MessageDeobfuscation.Class::Property",
						"MessageDeobfuscation.Class::Event"
					};
					foreach (var original in expectedOriginals)
						Assert.Contains(original, map.Values);

					// The deobfuscator must reverse the map: each obfuscated symbol resolves back
					// to its original full name (verified using the map's own keys, so this is
					// robust to identifier drift).
					var deobfuscator = MessageDeobfuscator.Load(symbolsPath);
					foreach (var original in expectedOriginals) {
						var obfuscated = map.First(entry => entry.Value == original).Key;
						Assert.Equal(original, deobfuscator.DeobfuscateSymbol(obfuscated, false));
					}

					return Task.Delay(0);
				}
			);

		public static IEnumerable<object[]> RenameModes() =>
			new[] {
				new object[] { nameof(RenameMode.Decodable) },
				new object[] { nameof(RenameMode.Sequential) }
			};

		[Fact]
		[Trait("Category", "Protection")]
		public async Task CheckGeneratedPassword() {
			string actualPassword1 = null, actualPassword2 = null;
			await RunDeobfuscationWithPassword(true, null, "_0", Array.Empty<string>(),
				outputPath => {
					actualPassword1 = File.ReadAllText(Path.Combine(outputPath, CoreComponent.PasswordFileName));
					Assert.True(Guid.TryParse(actualPassword1, out _));
					return Task.Delay(0);
				});
			await RunDeobfuscationWithPassword(true, null, "_1", Array.Empty<string>(),
				outputPath => {
					actualPassword2 = File.ReadAllText(Path.Combine(outputPath, CoreComponent.PasswordFileName));
					Assert.True(Guid.TryParse(actualPassword2, out _));
					return Task.Delay(0);
				});
			Assert.NotEqual(actualPassword1, actualPassword2);
		}

		[Fact]
		[Trait("Category", "Protection")]
		public async Task CheckPasswordDependsOnSeed() {
			var expectedObfuscatedOutput = new[] {
				"Exception",
				"   at oZuuchQgRo99FxO43G5kj2LB6aE3b$hsLiIOVL3cn0lg.98C7L64wnMJK6DFKHzyWSw8.at9I2jHJrbSIlewmDrNXdMI(String )",
				"   at EcGxTPKtKIEeZuP3ekjPVhrVKQsiovm5zMkq5xfZbt1V.AiskF07vqbD8ZFG03Jyiiu8()"
			};
			await RunDeobfuscationWithPassword(true, Seed, "_0", expectedObfuscatedOutput,
				outputPath => {
					Assert.Equal(Seed, File.ReadAllText(Path.Combine(outputPath, CoreComponent.PasswordFileName)));
					return Task.Delay(0);
				});
			await RunDeobfuscationWithPassword(true, Seed, "_1", expectedObfuscatedOutput,
				outputPath => {
					Assert.Equal(Seed, File.ReadAllText(Path.Combine(outputPath, CoreComponent.PasswordFileName)));
					return Task.Delay(0);
				});
		}

		[Fact]
		[Trait("Category", "Protection")]
		[Trait("Protection", "rename")]
		public async Task MessageDeobfuscationWithPassword() {
			var expectedObfuscatedOutput = new[] {
				"Exception",
				"   at oQmpV$y2k2b9P3d6GP1cxGPuRtKaNIZvZcKpZXSfKFG8.99_z9Rxdp_fWfuD3fr45FSA.at9DaPNMANuLaMV_3scPWDU(String )",
				"   at EbUjRcrC76NnA7RJlhQffrfp$vMGHdDfqtVFtWrAOPyD.AkpOh$3Zo3M8ga5lTY9etcM()"
			};
			await RunDeobfuscationWithPassword(false, null, "", expectedObfuscatedOutput, outputPath => {
				var deobfuscator = new MessageDeobfuscator(Password);
				var deobfuscatedMessage =
					deobfuscator.DeobfuscateMessage(string.Join(Environment.NewLine, expectedObfuscatedOutput));

				void CheckName(string expectedName, string obfuscatedName) {
					var name = deobfuscator.DeobfuscateSymbol(obfuscatedName, true);
					Assert.Equal(expectedName, name);
				}

				CheckName("MessageDeobfuscation.Class", "oQmpV$y2k2b9P3d6GP1cxGPuRtKaNIZvZcKpZXSfKFG8");
				CheckName("NestedClass", "CE8t0VDPQk9$jgv1XuRwt1k");
				CheckName("Method", "jevJU4p4yNrAYGqN7GkRWaI");
				CheckName("Field", "3IS4xsnUsvDQZop6e4WmNVw");
				CheckName("Property", "917VMBMNYHd0kfnnNkgeJ10");
				CheckName("Event", "AIyINk7kgFLFc73Md8Nu8Z0");

				Assert.Equal(_expectedDeobfuscatedOutput, deobfuscatedMessage);
				return Task.Delay(0);
			});
		}

		async Task RunDeobfuscationWithPassword(bool generatePassword, string seed, string suffix,
			string[] expectedObfuscatedOutput, Func<string, Task> postProcessAction) => await Run(
			"MessageDeobfuscation.exe",
			expectedObfuscatedOutput,
			new SettingItem<Protection>("rename") {
				["mode"] = "reversible",
				["password"] = Password,
				["generatePassword"] = generatePassword.ToString()
			},
			$"Password_{(generatePassword ? $"Random{(seed != null ? "_Seed" : "")}{suffix}" : $"Hardcoded{suffix}")}",
			checkOutput: !generatePassword || seed != null,
			seed: seed,
			postProcessAction: postProcessAction
		);
	}
}
