using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	Confuser.Analyzers.ResolveThrowAuditAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Confuser.Analyzers.Test {
	public class ResolveThrowAuditAnalyzerTest {
		[Theory]
		[InlineData("ResolveThrow")]
		[InlineData("ResolveTypeDefThrow")]
		[InlineData("ResolveMethodDefThrow")]
		[InlineData("ResolveFieldThrow")]
		public async Task Flags_ResolveThrowHelpers(string methodName) {
			string source = @"
class Ref { public object " + methodName + @"() => null; }
class C {
    void M(Ref r) {
        var x = r.{|#0:" + methodName + @"|}();
    }
}";
			var expected = Verify.Diagnostic("CX003").WithLocation(0).WithArguments(methodName);
			await Verify.VerifyAnalyzerAsync(source, expected);
		}

		[Fact]
		public async Task DoesNotFlag_NonThrowingResolve() {
			const string source = @"
class Ref { public object ResolveTypeDef() => null; }
class C {
    void M(Ref r) {
        var x = r.ResolveTypeDef();
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}
	}
}
