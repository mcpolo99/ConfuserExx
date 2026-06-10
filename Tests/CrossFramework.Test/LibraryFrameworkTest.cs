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
		[Trait("TFM", "net8.0")]
		public Task Library_Net8_RenameProtection() =>
			Run("CrossFramework.Library.Net8.dll",
				null,
				new SettingItem<Protection>("rename"),
				outputDirSuffix: "-lib-net8",
				checkOutput: false);
	}
}
