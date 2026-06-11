using System.IO;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
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
	}
}
