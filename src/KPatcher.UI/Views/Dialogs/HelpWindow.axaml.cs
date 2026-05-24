using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using KPatcher.UI.Parity;

namespace KPatcher.UI.Views.Dialogs
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            AvaloniaXamlLoader.Load(this);
            TextBlock parityLedgerTextBlock = this.FindControl<TextBlock>("ParityLedgerTextBlock");
            if (parityLedgerTextBlock != null)
            {
                parityLedgerTextBlock.Text = ParityLedger.BuildReport();
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
