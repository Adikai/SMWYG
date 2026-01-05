using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SMWYG.Models;
using SMWYG.Utils;

namespace SMWYG
{
    public partial class LoginWindow : Window
    {
        private readonly AppDbContext _db;
        public User? SignedInUser { get; private set; }

        public LoginWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
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

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null)
            {
                ShowSignInError("Invalid credentials.");
                return;
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ShowSignInError("Account is deactivated. Contact an administrator.");
                return;
            }

            if (!PasswordHelper.VerifyPassword(user.PasswordHash, password))
            {
                ShowSignInError("Invalid credentials.");
                return;
            }

            SignedInUser = user;
            DialogResult = true;
            Close();
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

            bool usernameExists = await _db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            if (usernameExists)
            {
                ShowRegisterError("Username already exists.");
                return;
            }

            string normalizedToken = tokenInput.ToUpperInvariant();
            var invite = await _db.InviteTokens.FirstOrDefaultAsync(t => t.Token.ToUpper() == normalizedToken);
            if (invite == null)
            {
                ShowRegisterError("Invalid invite token.");
                return;
            }

            if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value <= DateTime.UtcNow)
            {
                ShowRegisterError("Invite token has expired.");
                return;
            }

            if (invite.IsUsed && invite.MaxUses <= 0)
            {
                ShowRegisterError("Invite token has already been used.");
                return;
            }

            if (invite.MaxUses <= 0)
            {
                ShowRegisterError("Invite token has no remaining uses.");
                return;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
                PasswordHash = PasswordHelper.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                IsAdmin = false
            };

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                invite.MaxUses = Math.Max(0, invite.MaxUses - 1);
                if (invite.MaxUses == 0)
                {
                    invite.IsUsed = true;
                    invite.UsedBy = user.Id;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                ShowRegisterError("Registration failed. Try again.");
                return;
            }

            SignedInUser = user;
            DialogResult = true;
            Close();
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