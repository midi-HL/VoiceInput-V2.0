using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VoiceInput.Pages
{
    public sealed partial class ApiSettingsPage : Page
    {
        public ApiSettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            ApiBaseUrlBox.Text = Settings.ApiBaseUrl;
            ApiKeyBox.Password = Settings.ApiKey;
            TranscriptionModelBox.Text = Settings.TranscriptionModel;
            LlmModelBox.Text = Settings.LlmModel;
            LlmCorrectionToggle.IsOn = Settings.LlmCorrectionEnabled;
        }

        private void ApiBaseUrl_Changed(object sender, TextChangedEventArgs e)
        {
            Settings.ApiBaseUrl = ApiBaseUrlBox.Text;
        }

        private void ApiKey_Changed(object sender, RoutedEventArgs e)
        {
            Settings.ApiKey = ApiKeyBox.Password;
        }

        private void TranscriptionModel_Changed(object sender, TextChangedEventArgs e)
        {
            Settings.TranscriptionModel = TranscriptionModelBox.Text;
        }

        private void LlmModel_Changed(object sender, TextChangedEventArgs e)
        {
            Settings.LlmModel = LlmModelBox.Text;
        }

        private void LlmCorrection_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.LlmCorrectionEnabled = LlmCorrectionToggle.IsOn;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            TestConnectionButton.IsEnabled = false;
            TestResultText.Text = "测试中...";
            TestResultText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

            try
            {
                var refiner = new LlmRefiner();
                bool success = await refiner.TestConnectionAsync();
                refiner.Dispose();

                if (success)
                {
                    TestResultText.Text = "连接成功";
                    TestResultText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }
                else
                {
                    TestResultText.Text = "连接失败，请检查配置";
                    TestResultText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            }
            catch
            {
                TestResultText.Text = "连接失败";
                TestResultText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }
    }
}
