
namespace HsmodConfiguration
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                window.Title = "HsMod 插件管理器";// System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            }

            return window;

        }
    }
}
