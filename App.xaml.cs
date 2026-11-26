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

        // Windows Phone 8.1 returns from the save picker here instead of to the caller.
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            var save = args as FileSavePickerContinuationEventArgs;
            if (save == null) return;
            // The phone may have closed the app while the picker was open, so rebuild the page if needed.
            var frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                frame = new Frame();
                Window.Current.Content = frame;
            }
            if (frame.Content == null) frame.Navigate(typeof(MainPage));
            Window.Current.Activate();
            var page = frame.Content as MainPage;
            if (page != null) await page.ContinueExportAsync(save.File);
        }
    }
}
