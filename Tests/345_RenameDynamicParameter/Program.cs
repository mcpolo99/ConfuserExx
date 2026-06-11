using System;

namespace RenameDynamicParameter {
	public class Program {
		internal static int Main(string[] args) {
			Console.WriteLine("START");

			SimpleTestClass.TestStatic();
			SimpleTestClass.TestDynamic();

			OverloadTestClass.TestString();
			OverloadTestClass.TestInteger();

			var overrideTest = new OverrideTestClass();
			overrideTest.TestString();
			overrideTest.TestInteger();

			var fieldTest = new FieldTestClass();
			fieldTest.TestDynamic();

			ConstructorTestClass.TestString();
			ConstructorTestClass.TestInteger();

			Console.WriteLine("END");

			return 42;
		}
	}
}
