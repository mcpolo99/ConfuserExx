using System;
using System.Text;

namespace Confuser.Core.Diagnostics {
	/// <summary>
	///     Scrubs sensitive information from text destined for a diagnostic report.
	/// </summary>
	/// <remarks>
	///     Diagnostic reports are meant to be pasted into public issue trackers, so any text
	///     that flows into one must have the reporter's identity removed. The most common leak
	///     is the user-profile path (e.g. <c>C:\Users\alice\...</c>) which appears in absolute
	///     paths throughout log output and project configuration.
	/// </remarks>
	public static class DiagnosticRedactor {
		/// <summary>
		///     The placeholder substituted for the user-profile directory.
		/// </summary>
		public const string UserPlaceholder = "%USER%";

		/// <summary>
		///     Replaces every occurrence of the user-profile directory in <paramref name="text" />
		///     with <see cref="UserPlaceholder" />. The match is case-insensitive because Windows
		///     paths are.
		/// </summary>
		/// <param name="text">The text to scrub. Returned unchanged if <c>null</c> or empty.</param>
		/// <param name="userProfile">The user-profile directory to redact, or <c>null</c> to skip.</param>
		/// <returns>The scrubbed text.</returns>
		public static string Redact(string text, string userProfile) {
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(userProfile))
				return text;
			return ReplaceCaseInsensitive(text, userProfile, UserPlaceholder);
		}

		static string ReplaceCaseInsensitive(string input, string search, string replacement) {
			var sb = new StringBuilder(input.Length);
			int index = 0;
			while (true) {
				int found = input.IndexOf(search, index, StringComparison.OrdinalIgnoreCase);
				if (found < 0) {
					sb.Append(input, index, input.Length - index);
					break;
				}

				sb.Append(input, index, found - index);
				sb.Append(replacement);
				index = found + search.Length;
			}

			return sb.ToString();
		}
	}
}
