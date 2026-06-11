using System;

namespace InterfaceRenamingLoop {
	public class Program {
		internal static int Main(string[] args) {
			Console.WriteLine("START");

			var test = new ClassB();
			test.TestEvent(0, "TEST");

			Console.WriteLine("END");

			return 42;
		}
	}
}
