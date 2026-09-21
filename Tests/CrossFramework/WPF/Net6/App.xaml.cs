using System;
using System.Windows;

namespace CrossFramework.WPF {
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			if (e.Args.Length > 0 && e.Args[0] == "--verify") {
				Console.WriteLine("START");
				var window = new MainWindow();
				Console.WriteLine("Title: " + window.Title);
				Console.WriteLine("Content: " + window.GetStatusText());
				Console.WriteLine("END");
				Shutdown(42);
				return;
			}

			if (e.Args.Length > 0 && (e.Args[0] == "--selftest" || e.Args[0] == "--selftest-crash")) {
				CrossFramework.SelfTest.WpfSelfTestHost.Run(this, new MainWindow(), induceCrash: e.Args[0] == "--selftest-crash");
				return;
			}

			new MainWindow().Show();
		}
	}
}
