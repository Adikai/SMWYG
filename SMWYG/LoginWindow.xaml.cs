using System;
using System.Windows;
using System.Windows.Controls;
using SMWYG.Models;
using SMWYG.Services;

namespace SMWYG
{
    public partial class LoginWindow : Window
    {
        private readonly IApiService _api;
        public User? SignedInUser { get; private set; }

        public LoginWindow(IApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            HideSignInError();

            var usernameBox = GetTextBox("UsernameBox");
            var passwordBox = GetPasswordBox("PasswordBox");
            if (usernameBox == null || passwordBox == null)
                return;

            string username = usernameBox.Text?.Trim() ?? string.Empty;
            string password = passwordBox.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowSignInError("Enter username and password.");
                return;
            }

            try
            {
                var user = await _api.LoginAsync(username, password);
                if (user == null)
                {
                    ShowSignInError("Invalid credentials.");
                    return;
                }

                SignedInUser = user;
                DialogResult = true;
                Close();
            }
            catch
            {
                ShowSignInError("Sign in failed.");
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            HideRegisterError();

            var inviteTokenBox = GetTextBox("InviteTokenBox");
            var newUsernameBox = GetTextBox("NewUsernameBox");
            var displayNameBox = GetTextBox("DisplayNameBox");
            var newPasswordBox = GetPasswordBox("NewPasswordBox");
            var confirmPasswordBox = GetPasswordBox("ConfirmPasswordBox");

            if (inviteTokenBox == null || newUsernameBox == null || newPasswordBox == null || confirmPasswordBox == null)
            {
                ShowRegisterError("Registration form is not available.");
                return;
            }

            string tokenInput = inviteTokenBox.Text?.Trim() ?? string.Empty;
            string username = newUsernameBox.Text?.Trim() ?? string.Empty;
            string displayName = displayNameBox?.Text?.Trim() ?? string.Empty;
            string password = newPasswordBox.Password ?? string.Empty;
            string confirmPassword = confirmPasswordBox.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tokenInput) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowRegisterError("All required fields must be filled.");
                return;
            }

            if (!password.Equals(confirmPassword, StringComparison.Ordinal))
            {
                ShowRegisterError("Passwords do not match.");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegisterError("Password must be at least 6 characters.");
                return;
            }

            try
            {
                string effectiveDisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
                var user = await _api.RegisterAsync(tokenInput.Trim(), username.Trim(), effectiveDisplayName.Trim(), password);
                if (user == null)
                {
                    ShowRegisterError("Registration failed.");
                    return;
                }

                SignedInUser = user;
                DialogResult = true;
                Close();
            }
            catch
            {
                ShowRegisterError("Registration failed.");
            }
        }

        private void ShowSignInError(string message)
        {
            var errorText = GetTextBlock("ErrorText");
            if (errorText == null)
                return;

            errorText.Text = message;
            errorText.Visibility = Visibility.Visible;
        }

        private void HideSignInError()
        {
            var errorText = GetTextBlock("ErrorText");
            if (errorText != null)
            {
                errorText.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowRegisterError(string message)
        {
            var errorText = GetTextBlock("RegisterErrorText");
            if (errorText == null)
                return;

            errorText.Text = message;
            errorText.Visibility = Visibility.Visible;
        }

        private void HideRegisterError()
        {
            var errorText = GetTextBlock("RegisterErrorText");
            if (errorText != null)
            {
                errorText.Visibility = Visibility.Collapsed;
            }
        }

        private TextBox? GetTextBox(string name) => FindName(name) as TextBox;
        private PasswordBox? GetPasswordBox(string name) => FindName(name) as PasswordBox;
        private TextBlock? GetTextBlock(string name) => FindName(name) as TextBlock;
    }
}