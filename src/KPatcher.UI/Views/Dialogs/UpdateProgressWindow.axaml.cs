using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KPatcher.UI.Views.Dialogs
{
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            AvaloniaXamlLoader.Load(this);
            ViewModel = new UpdateProgressViewModel();
            DataContext = ViewModel;
            CanResize = false;
        }

        public UpdateProgressViewModel ViewModel { get; }

        private bool _allowClose;

        public void AllowClose() => _allowClose = true;

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }
    }
}

