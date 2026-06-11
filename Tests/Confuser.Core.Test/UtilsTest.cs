using System.Collections.Generic;
using Xunit;

namespace Confuser.Core.Test {
	public class UtilsTest {
		[Theory]
		[MemberData(nameof(BuildRelativePathTestData))]
		[Trait("Issue", "https://github.com/mkaring/ConfuserEx/issues/413")]
		public void BuildRelativePath(string baseDirectory, string fileReference, string expectedRelativePath) =>
			Assert.Equal(expectedRelativePath, Utils.GetRelativePath(fileReference, baseDirectory), ignoreCase: true);

		public static IEnumerable<object[]> BuildRelativePathTestData() {
			yield return new object[] { "C:\\Test", "C:\\Test\\Asm.dll", "Asm.dll" };
			yield return new object[] { "C:\\Test\\", "C:\\Test\\Asm.dll", "Asm.dll" };
			yield return new object[] { "C:\\Test", "C:\\Test\\Test2\\Asm.dll", "Test2\\Asm.dll" };
			yield return new object[] { "C:\\Test\\", "C:\\Test\\Test2\\Asm.dll", "Test2\\Asm.dll" };
			yield return new object[] { "C:\\Test", "C:\\Test\\Test2\\Test3\\Asm.dll", "Test2\\Test3\\Asm.dll" };
			yield return new object[] { "C:\\Test\\", "C:\\Test\\Test2\\Test3\\Asm.dll", "Test2\\Test3\\Asm.dll" };
			yield return new object[] { "C:\\Test", "C:\\Test2\\Asm.dll", null };
			yield return new object[] { "C:\\Test\\", "C:\\Test2\\Asm.dll", null };

			// Only for case insensitive file systems (windows)
			yield return new object[] { "C:\\Test", "c:\\test\\test2\\test3\\asm.dll", "Test2\\Test3\\Asm.dll" };
			yield return new object[] { "C:\\Test", "C:\\TEST\\TEST2\\TEST3\\ASM.DLL", "Test2\\Test3\\Asm.dll" };
		}
	}
}
