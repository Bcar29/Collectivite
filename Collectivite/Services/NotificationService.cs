using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Collectivite.Services
{
    /// <summary>
    /// Notifications non bloquantes (Snackbar) affichées dans le host déclaré dans MainWindow.xaml.
    /// </summary>
    public static class NotificationService
    {
        public static SnackbarMessageQueue MessageQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(4));

        private const byte BackgroundAlpha = 0xB0;

        public static void ShowSuccess(string message) =>
            Enqueue(message, PackIconKind.CheckCircle, Color.FromRgb(0x38, 0x8E, 0x3C), TimeSpan.FromSeconds(4));

        public static void ShowInfo(string message) =>
            Enqueue(message, PackIconKind.InformationOutline, Color.FromRgb(0x45, 0x60, 0x76), TimeSpan.FromSeconds(4));

        public static void ShowWarning(string message) =>
            Enqueue(message, PackIconKind.AlertOutline, Color.FromRgb(0xF5, 0x7C, 0x00), TimeSpan.FromSeconds(6));

        public static void ShowError(string message) =>
            Enqueue(message, PackIconKind.AlertCircleOutline, Color.FromRgb(0xD3, 0x2F, 0x2F), TimeSpan.FromSeconds(8));

        private static void Enqueue(string message, PackIconKind icon, Color color, TimeSpan duration)
        {
            void DoEnqueue()
            {
                var background = Color.FromArgb(BackgroundAlpha, color.R, color.G, color.B);

                var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                stack.Children.Add(new PackIcon
                {
                    Kind = icon,
                    Foreground = Brushes.White,
                    Width = 26,
                    Height = 26,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 14, 0)
                });
                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 15,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 420
                });

                var content = new Border
                {
                    Background = new SolidColorBrush(background),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 16, 20, 16),
                    MinHeight = 56,
                    Child = stack
                };

                MessageQueue.Enqueue(content, null, null, null, false, false, duration);
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                DoEnqueue();
            }
            else
            {
                dispatcher.Invoke(DoEnqueue);
            }
        }
    }
}
