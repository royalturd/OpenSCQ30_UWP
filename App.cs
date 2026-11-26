using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace SCQ30Mobile
{
    // Plain C# application class, no App.xaml: the app builds without the XAML compiler.
    // MainPage is set as the window content directly instead of through a Frame, because
    // Frame.Navigate needs the XAML type metadata that only the XAML compiler generates.
    sealed class App : Application
    {
        static void Main(string[] args)
        {
            Start(p => new App());
        }

        public App()
        {
            RequestedTheme = ApplicationTheme.Dark;
            UnhandledException += (s, e) => AppLog.Add("UNHANDLED " + e.Exception);
        }

        static MainPage EnsurePage()
        {
            var page = Window.Current.Content as MainPage;
            if (page == null)
            {
                page = new MainPage();
                Window.Current.Content = page;
            }
            Window.Current.Activate();
            return page;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            EnsurePage();
        }

        // Windows Phone 8.1 returns from the save picker here instead of to the caller.
        // The phone may have closed the app while the picker was open, so the page is rebuilt if needed.
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            var page = EnsurePage();
            var save = args as FileSavePickerContinuationEventArgs;
            if (save != null) await page.ContinueExportAsync(save.File);
        }
    }
}
