using System;
using System.Windows;

namespace CrossFramework.WPF {
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
		}

		private void OnButtonClick(object sender, RoutedEventArgs e) {
			statusText.Text = "Clicked!";
		}

		public string GetStatusText() {
			return statusText.Text;
		}
	}
}
