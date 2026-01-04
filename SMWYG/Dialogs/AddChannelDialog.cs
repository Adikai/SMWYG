using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SMWYG.Dialogs
{
    public class AddChannelDialog : Window
    {
        private readonly TextBox channelNameTextBox;
        private readonly RadioButton textChannelRadio;
        private readonly RadioButton voiceChannelRadio;

        public string ChannelName { get; private set; } = string.Empty;
        public string ChannelType { get; private set; } = "text";

        public AddChannelDialog(string defaultType = "text")
        {
            Title = "Create Channel";
            Width = 360;
            Height = 260;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = CreateBrush("#2F3136");
            Foreground = Brushes.White;
            WindowStyle = WindowStyle.ToolWindow;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new TextBlock
            {
                Text = "Channel name",
                Foreground = CreateBrush("#B9BBBE"),
                FontSize = 12
            };
            grid.Children.Add(nameLabel);

            channelNameTextBox = new TextBox
            {
                Margin = new Thickness(0, 4, 0, 0),
                Background = CreateBrush("#40444B"),
                Foreground = Brushes.White,
                BorderBrush = CreateBrush("#292B2F"),
                Padding = new Thickness(6),
                MaxLength = 100
            };
            Grid.SetRow(channelNameTextBox, 1);
            grid.Children.Add(channelNameTextBox);

            var typeLabel = new TextBlock
            {
                Text = "Channel type",
                Foreground = CreateBrush("#B9BBBE"),
                FontSize = 12
            };
            Grid.SetRow(typeLabel, 3);
            grid.Children.Add(typeLabel);

            var typePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetRow(typePanel, 4);

            textChannelRadio = new RadioButton
            {
                Content = "Text",
                Margin = new Thickness(0, 0, 16, 0),
                GroupName = "ChannelType",
                Foreground = Brushes.White
            };
            textChannelRadio.Checked += OnChannelTypeChanged;
            typePanel.Children.Add(textChannelRadio);

            voiceChannelRadio = new RadioButton
            {
                Content = "Voice",
                GroupName = "ChannelType",
                Foreground = Brushes.White
            };
            voiceChannelRadio.Checked += OnChannelTypeChanged;
            typePanel.Children.Add(voiceChannelRadio);

            grid.Children.Add(typePanel);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 24, 0, 0)
            };
            Grid.SetRow(actions, 5);

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

            var createButton = new Button
            {
                Content = "Create",
                Width = 80,
                Background = CreateBrush("#3BA55D"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsDefault = true
            };
            createButton.Click += (_, _) => ConfirmSelection();
            actions.Children.Add(createButton);

            grid.Children.Add(actions);
            Content = grid;

            ChannelType = string.Equals(defaultType, "voice", StringComparison.OrdinalIgnoreCase) ? "voice" : "text";
            if (ChannelType == "voice")
            {
                voiceChannelRadio.IsChecked = true;
            }
            else
            {
                textChannelRadio.IsChecked = true;
            }

            Loaded += (_, _) =>
            {
                channelNameTextBox.Focus();
                channelNameTextBox.SelectAll();
            };
        }

        private void OnChannelTypeChanged(object sender, RoutedEventArgs e)
        {
            ChannelType = voiceChannelRadio.IsChecked == true ? "voice" : "text";
        }

        private void ConfirmSelection()
        {
            ChannelName = channelNameTextBox.Text.Trim();
            ChannelType = voiceChannelRadio.IsChecked == true ? "voice" : "text";
            DialogResult = true;
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        }
    }
}
