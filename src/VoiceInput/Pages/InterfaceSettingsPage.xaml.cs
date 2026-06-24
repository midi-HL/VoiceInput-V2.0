using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VoiceInput.Pages
{
    public sealed partial class InterfaceSettingsPage : Page
    {
        public InterfaceSettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            ChineseRadio.IsChecked = Settings.AppLanguage == AppLanguage.Chinese;
            EnglishRadio.IsChecked = Settings.AppLanguage == AppLanguage.English;

            HudOffsetSlider.Value = Settings.HudOffsetY;

            MinimizeRadio.IsChecked = Settings.CloseBehavior == CloseBehavior.MinimizeToTray;
            CloseRadio.IsChecked = Settings.CloseBehavior == CloseBehavior.CloseApp;
        }

        private void AppLanguage_Changed(object sender, RoutedEventArgs e)
        {
            if (ChineseRadio.IsChecked == true)
                Settings.AppLanguage = AppLanguage.Chinese;
            else if (EnglishRadio.IsChecked == true)
                Settings.AppLanguage = AppLanguage.English;
        }

        private void HudOffset_Changed(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Settings.HudOffsetY = (int)e.NewValue;
        }

        private void CloseBehavior_Changed(object sender, RoutedEventArgs e)
        {
            if (MinimizeRadio.IsChecked == true)
                Settings.CloseBehavior = CloseBehavior.MinimizeToTray;
            else if (CloseRadio.IsChecked == true)
                Settings.CloseBehavior = CloseBehavior.CloseApp;
        }
    }
}
