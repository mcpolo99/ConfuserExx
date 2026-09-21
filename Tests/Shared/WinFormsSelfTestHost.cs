using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossFramework.SelfTest {
	// Shared across the WinForms subject apps (linked, not duplicated). Actually runs the app:
	// shows the real main window off-screen, lets the message loop pump briefly, then closes it.
	// Any unhandled exception (UI thread or background) makes the process report failure.
	//
	// Contract: prints START / SHOWN / END and exits 42 on a clean run; on a crash prints
	// "CRASH: ..." and exits 1. The window is placed off-screen with no taskbar entry so it is
	// non-intrusive on a developer machine while still exercising a genuine window + message loop.
	internal static class WinFormsSelfTestHost {
		public static int Run(Func<Form> formFactory, bool induceCrash = false) {
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
				Fail("AppDomain", e.ExceptionObject as Exception);
			Application.ThreadException += (sender, e) =>
				Fail("ThreadException", e.Exception);
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

			Console.WriteLine("START");

			var form = formFactory();
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(-32000, -32000);
			form.ShowInTaskbar = false;

			form.Shown += (sender, e) => {
				Console.WriteLine("SHOWN: " + form.Text);
				if (induceCrash)
					throw new InvalidOperationException("Induced self-test crash");

				var timer = new Timer { Interval = 2000 };
				timer.Tick += (s, args) => {
					timer.Stop();
					form.Close();
				};
				timer.Start();
			};

			Application.Run(form);
			Console.WriteLine("END");
			return 42;
		}

		static void Fail(string source, Exception ex) {
			Console.WriteLine("CRASH: [" + source + "] " + (ex != null ? ex.Message : "unknown"));
			Console.Out.Flush();
			Environment.Exit(1);
		}
	}
}
