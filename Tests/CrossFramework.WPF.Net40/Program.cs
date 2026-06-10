using System;
using System.Windows;
using System.Windows.Controls;

namespace CrossFramework.WPF {
	public class MainWindow : Window {
		private TextBlock statusText;

		public MainWindow() {
			Title = "ConfuserEx WPF Test (net40)";
			Width = 300;
			Height = 200;

			statusText = new TextBlock { Text = "WPF is working", FontSize = 16 };
			var button = new Button { Content = "Click Me", Margin = new Thickness(0, 10, 0, 0) };
			button.Click += (s, e) => statusText.Text = "Clicked!";

			var panel = new StackPanel { Margin = new Thickness(20) };
			panel.Children.Add(statusText);
			panel.Children.Add(button);
			Content = panel;
		}

		public string GetStatusText() {
			return statusText.Text;
		}
	}

	static class Program {
		[STAThread]
		static int Main(string[] args) {
			if (args.Length > 0 && args[0] == "--verify") {
				Console.WriteLine("START");
				var window = new MainWindow();
				Console.WriteLine("Title: " + window.Title);
				Console.WriteLine("Content: " + window.GetStatusText());
				Console.WriteLine("END");
				return 42;
			}

			var app = new Application();
			app.Run(new MainWindow());
			return 0;
		}
	}
}
