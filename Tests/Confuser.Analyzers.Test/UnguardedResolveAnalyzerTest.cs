using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	Confuser.Analyzers.UnguardedResolveAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Confuser.Analyzers.Test {
	public class UnguardedResolveAnalyzerTest {
		const string Stub = @"
class Def { public int Field; public void Do() {} }
class Ref {
    public Def ResolveTypeDef() => null;
    public Def ResolveMethodDef() => null;
}
";

		[Fact]
		public async Task Flags_ImmediateMemberDereference() {
			string source = Stub + @"
class C {
    void M(Ref r) {
        r.{|#0:ResolveTypeDef|}().Do();
    }
}";
			var expected = Verify.Diagnostic("CX002").WithLocation(0).WithArguments("ResolveTypeDef");
			await Verify.VerifyAnalyzerAsync(source, expected);
		}

		[Fact]
		public async Task DoesNotFlag_NullConditionalDereference() {
			string source = Stub + @"
class C {
    void M(Ref r) {
        r.ResolveMethodDef()?.Do();
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task DoesNotFlag_AssignmentWithoutDereference() {
			string source = Stub + @"
class C {
    Def M(Ref r) {
        var d = r.ResolveTypeDef();
        return d;
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task DoesNotFlag_PassedAsArgument() {
			// Mirrors the real VTableAnalyzer usage: the result is handed to a null-tolerant method,
			// which is not a dereference and must not be flagged.
			string source = Stub + @"
class C {
    static bool AreEqual(Def a, Def b) => true;
    void M(Ref r) {
        var ok = AreEqual(r.ResolveTypeDef(), r.ResolveMethodDef());
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}
	}
}
