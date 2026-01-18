using LinkedLamp.Pages;

namespace LinkedLamp
{
    public partial class App : Application
    {
        private readonly BleScanPage _bleScanPage;

        // MAUI va injecter BleScanPage via DI
        public App(BleScanPage bleScanPage)
        {
            InitializeComponent();
            _bleScanPage = bleScanPage;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // NavigationPage => permet Navigation.PushAsync() entre pages
            return new Window(new NavigationPage(_bleScanPage));
        }
    }
}
