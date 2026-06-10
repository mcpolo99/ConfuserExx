using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;

namespace ConfuserEx.ViewModel {
	internal class AboutTabVM : TabViewModel {
		public AboutTabVM(AppVM app)
			: base(app, "About") {
			var decoder = new IconBitmapDecoder(new Uri("pack://application:,,,/ConfuserEx.ico"), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);

			Icon = decoder.Frames.First(frame => frame.Width == 64);
		}

		public ICommand LaunchBrowser {
			get { return new RelayCommand<string>(site => Process.Start(new ProcessStartInfo(site) { UseShellExecute = true })); }
		}

		public BitmapSource Icon { get; private set; }
	}
}