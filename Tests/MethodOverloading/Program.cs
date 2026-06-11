using System;

namespace A {
	public static class X {
	}
}

namespace B {
	public static class X {
	}
}

namespace MethodOverloading {
	public interface IInterface {
		string Method(string param);
	}

	public class BaseClass {
		public string Method(string param) => param;

		public virtual string VirtualMethod() => "BaseClassVirtualMethod";

		public string Field = "field";

		public string Property => "property";

		public event EventHandler<string> Event;
	}

	public class Class : BaseClass, IInterface {
		public override string VirtualMethod() => "ClassVirtualMethod";
	}

	public interface IInterface2<Result> {
		string Method2(Result param);
	}

	public class BaseClass2 {
		public string Method2(string param) => param;
	}

	public class Class2 : BaseClass2, IInterface2<string> {
	}

	public class Class3 {
		public string Method3(string param) => "class3";
	}

	public class Class4 {
		public string Method3(string param) => "class4";
	}

	public interface Interface5 {
		string Method5(string param);
	}

	public class BaseClass5<T> {
		public virtual T Method5(T param) => param;
	}

	public class Class5 : BaseClass5<string>, Interface5 {
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
	sealed class MyAttribute : Attribute {
		public MyAttribute(Type type) {
		}
	}

	[My(typeof(A.X))]
	[My(typeof(B.X))]
	public class Program {
		public class NestedClass {
			public override string ToString() => "test";
		}

		public static int OverloadedMethod(int param) => param;

		public static string OverloadedMethod(string param) => param;

		public static object OverloadedMethod(object[] objects) => objects[0];

		public static object OverloadedMethod(bool cond, float param1, double param2) => cond ? param1 : param2;

		public static NestedClass OverloadedMethod(NestedClass nestedClass) => nestedClass;

		public static T OverloadedMethod<T>(T param) => param;

		static int Main(string[] args) {
			Console.WriteLine("START");
			Console.WriteLine(OverloadedMethod(1));
			Console.WriteLine(OverloadedMethod("Hello world"));
			Console.WriteLine(OverloadedMethod(new object[] { "object" }));
			Console.WriteLine(OverloadedMethod(false, 1.0f, 2.0));
			Console.WriteLine(OverloadedMethod(new NestedClass()));
			Console.WriteLine(OverloadedMethod((byte)5));
			Console.WriteLine(new Class().Method("class"));
			Console.WriteLine(new Class2().Method2("class2"));
			Console.WriteLine(new Class3().Method3("class3"));
			Console.WriteLine(new Class4().Method3("class4"));
			Console.WriteLine(new Class5().Method5("class5"));
			Console.WriteLine(new BaseClass().VirtualMethod());
			BaseClass baseClass = new Class();
			Console.WriteLine(baseClass.VirtualMethod());
			Console.WriteLine(new Class().VirtualMethod());
			Console.WriteLine("END");
			return 42;
		}
	}
}
