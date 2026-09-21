using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CrossFramework.Console {
	class Program {
		static int Main() {
			System.Console.WriteLine("START");
			System.Console.WriteLine("Hello from net10.0");
			var resourceName = typeof(Program).Assembly.GetManifestResourceNames()
				.Single(name => name.EndsWith("Resource.txt", StringComparison.Ordinal));
			using (var stream = typeof(Program).Assembly.GetManifestResourceStream(resourceName))
			using (var reader = new StreamReader(stream)) {
				System.Console.WriteLine(reader.ReadToEnd().Trim());
			}
			System.Console.WriteLine("END");
			return 42;
		}
	}
}
