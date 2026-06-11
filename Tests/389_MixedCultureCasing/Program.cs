using System;
using System.Globalization;

namespace MixedCultureCasing {
	public class Program {
		internal static int Main(string[] args) {
			Console.WriteLine("START");

			Resource1.Culture = CultureInfo.GetCultureInfo("en-US");
			Console.WriteLine(Resource1.Test1);

			Resource1.Culture = CultureInfo.GetCultureInfo("de-DE");
			Console.WriteLine(Resource1.Test1);

			Resource2.Culture = CultureInfo.GetCultureInfo("en-US");
			Console.WriteLine(Resource2.Test2);

			Resource2.Culture = CultureInfo.GetCultureInfo("de-DE");
			Console.WriteLine(Resource2.Test2);

			Console.WriteLine("END");

			return 42;
		}
	}
}
