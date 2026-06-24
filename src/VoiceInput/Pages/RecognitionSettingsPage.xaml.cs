using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VoiceInput.Pages
{
    public sealed partial class RecognitionSettingsPage : Page
    {
        public RecognitionSettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            LocalRadio.IsChecked = Settings.RecognitionMode == RecognitionMode.LocalWithLlmCorrection;
            AiRadio.IsChecked = Settings.RecognitionMode == RecognitionMode.AiTranscription;

            LanguageComboBox.SelectedIndex = Settings.RecognitionLanguage switch
            {
                RecognitionLanguage.ZhCN => 0,
                RecognitionLanguage.EnUS => 1,
                RecognitionLanguage.ZhTW => 2,
                RecognitionLanguage.JaJP => 3,
                RecognitionLanguage.KoKR => 4,
                _ => 0
            };

            TriggerKeyComboBox.SelectedIndex = 0;
        }

        private void RecognitionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (LocalRadio.IsChecked == true)
                Settings.RecognitionMode = RecognitionMode.LocalWithLlmCorrection;
            else if (AiRadio.IsChecked == true)
                Settings.RecognitionMode = RecognitionMode.AiTranscription;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.RecognitionLanguage = LanguageComboBox.SelectedIndex switch
            {
                0 => RecognitionLanguage.ZhCN,
                1 => RecognitionLanguage.EnUS,
                2 => RecognitionLanguage.ZhTW,
                3 => RecognitionLanguage.JaJP,
                4 => RecognitionLanguage.KoKR,
                _ => RecognitionLanguage.ZhCN
            };
        }
    }
}
