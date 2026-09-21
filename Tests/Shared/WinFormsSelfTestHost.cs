using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossFramework.SelfTest {
	// Shared across the WinForms subject apps (linked, not duplicated). Actually runs the app:
	// shows the real main window (invisible + off-screen), lets the message loop pump briefly, then
	// ends the process. Any unhandled exception (UI thread or background) makes it report failure.
	//
	// Contract: prints START / SHOWN / END and exits 42 on a clean run; on a crash prints
	// "CRASH: ..." and exits 1. Termination is driven by a thread-pool watchdog rather than a UI
	// timer, so it always ends even if the message loop never closes the window (e.g. under
	// obfuscation, where the WinForms Timer proved unreliable).
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
			form.WindowState = FormWindowState.Minimized;
			form.Opacity = 0d;

			form.Shown += (sender, e) => {
				Console.WriteLine("SHOWN: " + form.Text);
				if (induceCrash)
					throw new InvalidOperationException("Induced self-test crash");
			};

			// Guaranteed terminator, independent of the UI message loop: once the window has had a
			// moment to come up (and crash if it is going to), end the process cleanly from a
			// thread-pool thread. Rooted across Application.Run so it cannot be collected first.
			var watchdog = new System.Threading.Timer(
				_ => Succeed(), null, 2000, System.Threading.Timeout.Infinite);

			Application.Run(form);
			GC.KeepAlive(watchdog);
			Succeed();
			return 42;
		}

		static void Succeed() {
			Console.WriteLine("END");
			Console.Out.Flush();
			Environment.Exit(42);
		}

		static void Fail(string source, Exception ex) {
			Console.WriteLine("CRASH: [" + source + "] " + (ex != null ? ex.Message : "unknown"));
			Console.Out.Flush();
			Environment.Exit(1);
		}
	}
}
