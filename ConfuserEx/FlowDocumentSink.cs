using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Serilog.Core;
using Serilog.Events;

namespace ConfuserEx {
	/// <summary>
	///     A Serilog sink that writes log events to a WPF <see cref="Paragraph" />
	///     with color-coded output matching the original ConfuserEx console style.
	/// </summary>
	internal sealed class FlowDocumentSink : ILogEventSink {
		readonly Paragraph paragraph;

		public FlowDocumentSink(Paragraph paragraph) {
			this.paragraph = paragraph ?? throw new ArgumentNullException(nameof(paragraph));
		}

		public void Emit(LogEvent logEvent) {
			var brush = GetBrush(logEvent.Level);
			var prefix = GetPrefix(logEvent.Level);
			var message = logEvent.RenderMessage();

			Application.Current.Dispatcher.BeginInvoke(new Action(() => {
				paragraph.Inlines.Add(new Run(prefix + message) { Foreground = brush });
				paragraph.Inlines.Add(new LineBreak());

				if (logEvent.Exception != null) {
					paragraph.Inlines.Add(new Run("Exception: " + logEvent.Exception) { Foreground = brush });
					paragraph.Inlines.Add(new LineBreak());
				}
			}));
		}

		static Brush GetBrush(LogEventLevel level) {
			switch (level) {
				case LogEventLevel.Verbose:
				case LogEventLevel.Debug:
					return Brushes.Gray;
				case LogEventLevel.Information:
					return Brushes.White;
				case LogEventLevel.Warning:
					return Brushes.Yellow;
				case LogEventLevel.Error:
				case LogEventLevel.Fatal:
					return Brushes.Red;
				default:
					return Brushes.White;
			}
		}

		static string GetPrefix(LogEventLevel level) {
			switch (level) {
				case LogEventLevel.Verbose:
				case LogEventLevel.Debug:
					return "[DEBUG] ";
				case LogEventLevel.Information:
					return " [INFO] ";
				case LogEventLevel.Warning:
					return " [WARN] ";
				case LogEventLevel.Error:
				case LogEventLevel.Fatal:
					return "[ERROR] ";
				default:
					return "";
			}
		}
	}
}
