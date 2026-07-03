using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using dnlib.DotNet;
using Xunit;
using Xunit.Abstractions;

namespace CrossFramework.Test {
	public class LibraryFrameworkTest : TestBase {
		public LibraryFrameworkTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Library")]
		[Trait("TFM", "netstandard2.0")]
		public Task Library_NetStd20_RenameProtection() =>
			Run("CrossFramework.Library.NetStd20.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-netstd20",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Library")]
		[Trait("TFM", "net48")]
		public Task Library_Net48_RenameProtection() =>
			Run("CrossFramework.Library.Net48.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-net48",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Library")]
		[Trait("TFM", "net6.0")]
		public Task Library_Net6_RenameProtection() =>
			Run("CrossFramework.Library.Net6.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-net6",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Library")]
		[Trait("TFM", "net8.0")]
		public Task Library_Net8_RenameProtection() =>
			Run("CrossFramework.Library.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-net8",
				checkOutput: false);

		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Library")]
		[Trait("TFM", "net10.0")]
		public Task Library_Net10_RenameProtection() =>
			Run("CrossFramework.Library.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-net10",
				checkOutput: false);

		// Regression guard: the subject has a 'allows ref struct' generic constraint
		// (GenericParamAttributes.AllowByRefLike). Renaming must not drop it — verify the flag
		// survives obfuscation on the output assembly. (dnlib 4.x names this flag; ConfuserEx
		// only renames generic parameters, so the attribute bits must be preserved.)
		[Fact]
		[Trait("Category", "CrossFramework")]
		[Trait("AppType", "Library")]
		[Trait("TFM", "net10.0")]
		public Task Library_Net10_PreservesAllowByRefLike() =>
			Run("CrossFramework.Library.Net10.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-net10-refstruct",
				checkOutput: false,
				postProcessAction: outputPath => {
					var modulePath = Path.Combine(outputPath, "CrossFramework.Library.Net10.dll");
					using var module = ModuleDefMD.Load(modulePath);
					var genericParams = module.GetTypes()
						.SelectMany(type => type.Methods)
						.SelectMany(method => method.GenericParameters)
						.Concat(module.GetTypes().SelectMany(type => type.GenericParameters));
					Assert.Contains(genericParams,
						gp => (gp.Flags & GenericParamAttributes.AllowByRefLike) != 0);
					return Task.CompletedTask;
				});
	}
}
