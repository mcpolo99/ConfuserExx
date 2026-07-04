using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using dnlib.DotNet;
using Xunit;
using Xunit.Abstractions;

namespace Watermark.Test {
	public sealed class WatermarkTest : TestBase {
		public WatermarkTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		// Issue #69: the watermark is opt-in. An obfuscation that does not request it must produce
		// output with no ConfusedByAttribute fingerprint.
		[Fact]
		[Trait("Category", "Protection")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/69")]
		public Task Watermark_NotRequested_ProducesNoFingerprintAttribute() =>
			Run("AntiTamper.exe",
				new[] { "This is a test." },
				new SettingItem<Protection>("rename"),
				"_wm_off",
				postProcessAction: outputPath => {
					using (var module = ModuleDefMD.Load(Path.Combine(outputPath, "AntiTamper.exe"))) {
						Assert.DoesNotContain(module.CustomAttributes, a => a.TypeFullName == "ConfusedByAttribute");
						Assert.DoesNotContain(module.GetTypes(), t => t.Name == "ConfusedByAttribute");
					}
					return Task.CompletedTask;
				});

		// When explicitly enabled with custom text and attribute name, the watermark must use them
		// (so it can be branded or disguised instead of the default ConfuserEx fingerprint).
		[Fact]
		[Trait("Category", "Protection")]
		[Trait("Issue", "https://github.com/mcpolo99/ConfuserExx/issues/69")]
		public Task Watermark_CustomTextAndName_AppliesConfiguredAttribute() =>
			Run("AntiTamper.exe",
				new[] { "This is a test." },
				new SettingItem<Protection>("watermark") {
					{ "text", "MyCorp Security" },
					{ "attributeName", "SecurityStampAttribute" }
				},
				"_wm_custom",
				postProcessAction: outputPath => {
					using (var module = ModuleDefMD.Load(Path.Combine(outputPath, "AntiTamper.exe"))) {
						var attr = module.CustomAttributes.FirstOrDefault(a => a.TypeFullName == "SecurityStampAttribute");
						Assert.NotNull(attr);
						Assert.Equal("MyCorp Security", attr.ConstructorArguments[0].Value?.ToString());

						// The default fingerprint name must not appear.
						Assert.DoesNotContain(module.CustomAttributes, a => a.TypeFullName == "ConfusedByAttribute");
					}
					return Task.CompletedTask;
				});
	}
}
