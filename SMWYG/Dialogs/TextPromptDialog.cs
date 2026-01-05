using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SMWYG.Dialogs
{
    public class TextPromptDialog : Window
    {
        private readonly TextBox inputTextBox;

        public string InputText { get; private set; } = string.Empty;

        public TextPromptDialog(string title, string prompt, string confirmButtonContent, string defaultValue = "")
        {
            Title = title;
            Width = 360;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = CreateBrush("#2F3136");
            Foreground = Brushes.White;
            WindowStyle = WindowStyle.ToolWindow;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptBlock = new TextBlock
            {
                Text = prompt,
                Foreground = CreateBrush("#B9BBBE"),
                FontSize = 12
            };
            grid.Children.Add(promptBlock);

            inputTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 0, 0),
                Background = CreateBrush("#40444B"),
                Foreground = Brushes.White,
                BorderBrush = CreateBrush("#292B2F"),
                Padding = new Thickness(6),
                MaxLength = 100,
                Text = defaultValue ?? string.Empty
            };
            Grid.SetRow(inputTextBox, 1);
            grid.Children.Add(inputTextBox);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(actions, 2);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Margin = new Thickness(0, 0, 12, 0),
                Background = CreateBrush("#4F545C"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsCancel = true
            };
            cancelButton.Click += (_, _) => DialogResult = false;
            actions.Children.Add(cancelButton);

            var confirmButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(confirmButtonContent) ? "OK" : confirmButtonContent,
                Width = 80,
                Background = CreateBrush("#3BA55D"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsDefault = true
            };
            confirmButton.Click += (_, _) => ConfirmInput();
            actions.Children.Add(confirmButton);

            grid.Children.Add(actions);
            Content = grid;

            Loaded += (_, _) =>
            {
                inputTextBox.Focus();
                inputTextBox.SelectAll();
            };
        }

        private void ConfirmInput()
        {
            InputText = inputTextBox.Text.Trim();
            DialogResult = true;
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        }
    }
}
