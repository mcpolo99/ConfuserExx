using System;

namespace ComplexClassStructureRename.Lib {
	internal class InternalClass1 : InternalBaseClass {
		public new void FireLog(string message) =>
			Console.WriteLine("InternalClass1: " + message);
	}
}
