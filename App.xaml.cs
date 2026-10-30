using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SCQ30Mobile
{
    sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UnhandledException += (s, e) => AppLog.Add("UNHANDLED " + e.Exception);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                frame = new Frame();
                Window.Current.Content = frame;
            }
            if (frame.Content == null) frame.Navigate(typeof(MainPage), e.Arguments);
            Window.Current.Activate();
        }
    }
}
