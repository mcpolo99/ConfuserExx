using System;
using System.Collections.Generic;

namespace CrossFramework.Library {
	public class SampleService {
		private readonly string prefix;

		public SampleService(string prefix) {
			this.prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
		}

		public string Prefix => prefix;

		public string Format(string input) {
			return prefix + ": " + input;
		}

		public IReadOnlyList<string> FormatMany(IEnumerable<string> inputs) {
			var results = new List<string>();
			foreach (var input in inputs)
				results.Add(Format(input));
			return results;
		}
	}

	public interface IProcessor {
		string Process(string data);
	}

	public class UpperCaseProcessor : IProcessor {
		public string Process(string data) {
			return data.ToUpperInvariant();
		}
	}

	public class ReverseProcessor : IProcessor {
		public string Process(string data) {
			var chars = data.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}
	}

	// Carries a C# 13+ 'allows ref struct' generic constraint, which the compiler emits as
	// the GenericParamAttributes.AllowByRefLike flag. Used to verify obfuscation preserves it.
	public static class RefStructConsumer {
		public static void Consume<T>(T value) where T : allows ref struct {
			// Intentionally empty — the 'allows ref struct' constraint is what this exercises.
		}
	}
}
