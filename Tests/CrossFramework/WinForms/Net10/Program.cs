using System;
using System.Windows.Forms;
using CrossFramework.SelfTest;

namespace CrossFramework.WinForms {
	static class Program {
		[STAThread]
		static int Main(string[] args) {
			if (args.Length > 0 && args[0] == "--verify") {
				Console.WriteLine("START");
				var form = new MainForm();
				Console.WriteLine("Label: " + form.GetLabelText());
				Console.WriteLine("Title: " + form.Text);
				Console.WriteLine("END");
				form.Dispose();
				return 42;
			}

			if (args.Length > 0 && (args[0] == "--selftest" || args[0] == "--selftest-crash"))
				return WinFormsSelfTestHost.Run(() => new MainForm(), induceCrash: args[0] == "--selftest-crash");

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
			return 0;
		}
	}
}
