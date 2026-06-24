using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VoiceInput.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            SettingsFrame.Navigate(typeof(RecognitionSettingsPage));
        }

        private void SettingsNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                string? tag = item.Tag?.ToString();
                Type? pageType = tag switch
                {
                    "Recognition" => typeof(RecognitionSettingsPage),
                    "Api" => typeof(ApiSettingsPage),
                    "Interface" => typeof(InterfaceSettingsPage),
                    _ => typeof(RecognitionSettingsPage)
                };

                if (pageType != null && SettingsFrame.CurrentType != pageType)
                {
                    SettingsFrame.Navigate(pageType);
                }
            }
        }
    }
}
