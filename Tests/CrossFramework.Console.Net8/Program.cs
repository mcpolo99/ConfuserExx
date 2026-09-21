using System;

namespace CrossFramework.Console {
	class Program {
		static int Main(string[] args) {
			if (args.Length > 0 && (args[0] == "--selftest" || args[0] == "--selftest-crash"))
				return CrossFramework.SelfTest.ConsoleSelfTestHost.Run(args[0] == "--selftest-crash");

			System.Console.WriteLine("START");
			System.Console.WriteLine("Hello from net8.0");
			System.Console.WriteLine("END");
			return 42;
		}
	}
}
