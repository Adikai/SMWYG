using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SMWYG.Dialogs;
using SMWYG.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace SMWYG
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private Guid? _pendingChannelSelection;

        // Collections
        [ObservableProperty]
        private ObservableCollection<Server> servers = new();

        [ObservableProperty]
        private ObservableCollection<Channel> channels = new();

        [ObservableProperty]
        private ObservableCollection<Message> messages = new();

        private ObservableCollection<InviteToken> inviteTokens = new();
        public ObservableCollection<InviteToken> InviteTokens
        {
            get => inviteTokens;
            set => SetProperty(ref inviteTokens, value);
        }

        // Selected items
        [ObservableProperty]
        private Server? selectedServer;

        [ObservableProperty]
        private Channel? selectedChannel;

        // Input
        [ObservableProperty]
        private string messageInput = string.Empty;

        private bool isInviteManagerOpen;
        public bool IsInviteManagerOpen
        {
            get => isInviteManagerOpen;
            set => SetProperty(ref isInviteManagerOpen, value);
        }

        private string inviteMaxUsesInput = "1";
        public string InviteMaxUsesInput
        {
            get => inviteMaxUsesInput;
            set => SetProperty(ref inviteMaxUsesInput, value);
        }

        private string inviteExpiryDaysInput = "7";
        public string InviteExpiryDaysInput
        {
            get => inviteExpiryDaysInput;
            set => SetProperty(ref inviteExpiryDaysInput, value);
        }

        private bool isServerSettingsOpen;
        public bool IsServerSettingsOpen
        {
            get => isServerSettingsOpen;
            set => SetProperty(ref isServerSettingsOpen, value);
        }

        private string serverSettingsName = string.Empty;
        public string ServerSettingsName
        {
            get => serverSettingsName;
            set => SetProperty(ref serverSettingsName, value);
        }

        private string serverSettingsInviteCode = string.Empty;
        public string ServerSettingsInviteCode
        {
            get => serverSettingsInviteCode;
            set => SetProperty(ref serverSettingsInviteCode, value);
        }

        private bool isUserSettingsOpen;
        public bool IsUserSettingsOpen
        {
            get => isUserSettingsOpen;
            set => SetProperty(ref isUserSettingsOpen, value);
        }

        private string userSettingsUsername = string.Empty;
        public string UserSettingsUsername
        {
            get => userSettingsUsername;
            set => SetProperty(ref userSettingsUsername, value);
        }

        private string? userSettingsProfilePicturePath;
        public string? UserSettingsProfilePicturePath
        {
            get => userSettingsProfilePicturePath;
            set => SetProperty(ref userSettingsProfilePicturePath, value);
        }

        // Current user (hardcoded for now – replace with real login later)
        private readonly User currentUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Placeholder
            Username = "Adhil",
            IsAdmin = true
        };

        public User CurrentUser => currentUser;

        public MainViewModel(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
            UserSettingsUsername = currentUser.Username;
            UserSettingsProfilePicturePath = currentUser.ProfilePicture;

            // Load persisted user data (profile picture, username, isAdmin) from DB
            _ = LoadCurrentUserAsync();
        }

        private async Task LoadCurrentUserAsync()
        {
            try
            {
                var userEntity = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id);
                if (userEntity != null)
                {
                    // Ensure UI updates happen on UI thread
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        currentUser.Username = userEntity.Username;
                        currentUser.ProfilePicture = userEntity.ProfilePicture;
                        currentUser.IsAdmin = userEntity.IsAdmin;

                        UserSettingsUsername = currentUser.Username;
                        UserSettingsProfilePicturePath = currentUser.ProfilePicture;

                        OnPropertyChanged(nameof(CurrentUser));
                    });
                }
            }
            catch
            {
                // ignore load errors for now
            }
        }

        // Load all servers the current user is a member of
        [RelayCommand]
        public async Task LoadServersAsync()
        {
            var previousSelection = SelectedServer?.Id;
            SelectedServer = null;
            await PopulateServersAsync(previousSelection);
        }

        private async Task PopulateServersAsync(Guid? serverIdToSelect)
        {
            var userServers = await _db.Servers
                .Where(s => _db.ServerMembers.Any(sm => sm.ServerId == s.Id && sm.UserId == currentUser.Id))
                .OrderBy(s => s.Name)
                .ToListAsync();

            Servers.Clear();
            foreach (var server in userServers)
            {
                Servers.Add(server);
            }

            if (serverIdToSelect.HasValue)
            {
                var match = Servers.FirstOrDefault(s => s.Id == serverIdToSelect.Value);
                if (match != null)
                {
                    SelectedServer = match;
                    return;
                }
            }

            if (Servers.Any() && SelectedServer == null)
            {
                SelectedServer = Servers.First();
            }
            else if (!Servers.Any())
            {
                SelectedServer = null;
                Channels.Clear();
                SelectedChannel = null;
                Messages.Clear();
            }
        }

        // When a server is selected → load its channels
        partial void OnSelectedServerChanged(Server? value)
        {
            if (value != null)
            {
                UpdateServerSettingsSnapshot(value);
                _ = LoadChannelsAsync(value.Id, _pendingChannelSelection);
                _pendingChannelSelection = null;
            }
            else
            {
                IsServerSettingsOpen = false;
                ServerSettingsName = string.Empty;
                ServerSettingsInviteCode = string.Empty;
                Channels.Clear();
                SelectedChannel = null;
                Messages.Clear();
            }
        }

        private async Task LoadChannelsAsync(Guid serverId, Guid? channelToSelect = null)
        {
            var serverChannels = await _db.Channels
                .Where(c => c.ServerId == serverId)
                .OrderBy(c => c.Position)
                .ThenBy(c => c.Name)
                .ToListAsync();

            Channels.Clear();
            foreach (var channel in serverChannels)
            {
                Channels.Add(channel);
            }

            Channel? nextChannel = null;
            if (channelToSelect.HasValue)
            {
                nextChannel = Channels.FirstOrDefault(c => c.Id == channelToSelect.Value);
            }

            if (nextChannel == null)
            {
                nextChannel = Channels.FirstOrDefault(c => c.Type == "text") ?? Channels.FirstOrDefault();
            }

            SelectedChannel = nextChannel;

            if (nextChannel == null)
            {
                Messages.Clear();
            }
        }

        // When a channel is selected → load messages if it's a text channel
        partial void OnSelectedChannelChanged(Channel? value)
        {
            if (value != null && value.Type == "text")
            {
                _ = LoadMessagesAsync(value.Id);
            }
            else
            {
                Messages.Clear();
            }
        }

        private async Task LoadMessagesAsync(Guid channelId)
        {
            var channelMessages = await _db.Messages
                .Where(m => m.ChannelId == channelId && m.DeletedAt == null)
                .Include(m => m.Author)
                .OrderBy(m => m.SentAt)
                .Take(100) // Limit for performance – implement pagination later
                .ToListAsync();

            Messages.Clear();
            foreach (var msg in channelMessages)
            {
                Messages.Add(msg);
            }

            // Scroll to bottom (you can trigger this via event or behavior later)
        }

        // Create a new server
        [RelayCommand]
        private async Task CreateServerAsync()
        {
            // Simple prompt – replace with proper dialog later
            string? name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter server name:", "Create New Server", "My Awesome Server");

            if (string.IsNullOrWhiteSpace(name))
                return;

            var newServer = new Server
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                OwnerId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.Servers.Add(newServer);

            // Add current user as owner/member
            _db.ServerMembers.Add(new ServerMember
            {
                ServerId = newServer.Id,
                UserId = currentUser.Id,
                Role = "owner",
                JoinedAt = DateTime.UtcNow
            });

            // Create default channels
            _db.Channels.Add(new Channel
            {
                Id = Guid.NewGuid(),
                ServerId = newServer.Id,
                Name = "general",
                Type = "text",
                Position = 0,
                CreatedAt = DateTime.UtcNow
            });

            _db.Channels.Add(new Channel
            {
                Id = Guid.NewGuid(),
                ServerId = newServer.Id,
                Name = "General",
                Type = "voice", // Group streaming channel
                Position = 1,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await ReloadServersPreservingSelectionAsync(newServer.Id);
        }

        [RelayCommand]
        private async Task UpdateServerIconAsync()
        {
            if (SelectedServer == null)
            {
                MessageBox.Show("Select a server first.", "No Server", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp",
                Title = "Select Server Icon"
            };

            bool? result = dialog.ShowDialog();
            if (result != true)
                return;

            var serverEntity = await _db.Servers.FirstOrDefaultAsync(s => s.Id == SelectedServer.Id);
            if (serverEntity == null)
            {
                MessageBox.Show("Server no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadServersAsync();
                return;
            }

            string iconsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerIcons");
            Directory.CreateDirectory(iconsDirectory);

            string extension = Path.GetExtension(dialog.FileName);
            string fileName = $"{serverEntity.Id}{extension}";
            string destinationPath = Path.Combine(iconsDirectory, fileName);
            File.Copy(dialog.FileName, destinationPath, true);

            string relativePath = Path.Combine("ServerIcons", fileName).Replace("\\", "/");
            serverEntity.Icon = relativePath;
            await _db.SaveChangesAsync();

            _pendingChannelSelection = SelectedChannel?.Id;
            await ReloadServersPreservingSelectionAsync(serverEntity.Id);
        }

        [RelayCommand]
        private async Task CreateChannelAsync()
        {
            if (SelectedServer == null)
            {
                MessageBox.Show("Select a server first.", "No Server", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new AddChannelDialog(SelectedChannel?.Type ?? "text")
            {
                Owner = Application.Current?.MainWindow
            };

            bool? result = dialog.ShowDialog();
            if (result != true)
                return;

            string channelName = dialog.ChannelName;
            if (string.IsNullOrWhiteSpace(channelName))
                return;

            string channelType = dialog.ChannelType;

            bool nameExists = await _db.Channels.AnyAsync(c => c.ServerId == SelectedServer.Id && c.Name.ToLower() == channelName.ToLower());
            if (nameExists)
            {
                MessageBox.Show("Channel name already exists for this server.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int nextPosition = (await _db.Channels
                .Where(c => c.ServerId == SelectedServer.Id)
                .Select(c => (int?)c.Position)
                .MaxAsync()) ?? -1;

            var newChannel = new Channel
            {
                Id = Guid.NewGuid(),
                ServerId = SelectedServer.Id,
                Name = channelName,
                Type = channelType,
                Position = nextPosition + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.Channels.Add(newChannel);
            await _db.SaveChangesAsync();

            await LoadChannelsAsync(SelectedServer.Id, newChannel.Id);
        }

        [RelayCommand]
        private async Task RenameChannelAsync()
        {
            if (SelectedServer == null || SelectedChannel == null)
            {
                MessageBox.Show("Select a channel first.", "No Channel", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string? newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the new channel name:", "Rename Channel", SelectedChannel.Name);
            if (string.IsNullOrWhiteSpace(newName))
                return;

            newName = newName.Trim();
            if (newName.Equals(SelectedChannel.Name, StringComparison.Ordinal))
                return;

            bool duplicate = await _db.Channels.AnyAsync(c =>
                c.ServerId == SelectedServer.Id &&
                c.Id != SelectedChannel.Id &&
                c.Name.ToLower() == newName.ToLower());
            if (duplicate)
            {
                MessageBox.Show("Channel name already exists for this server.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var channelEntity = await _db.Channels.FirstOrDefaultAsync(c => c.Id == SelectedChannel.Id);
            if (channelEntity == null)
            {
                MessageBox.Show("Channel no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadChannelsAsync(SelectedServer.Id);
                return;
            }

            channelEntity.Name = newName;
            await _db.SaveChangesAsync();

            await LoadChannelsAsync(SelectedServer.Id, channelEntity.Id);
        }

        // Send a message (Enter key in input box)
        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageInput) || SelectedChannel == null || SelectedChannel.Type != "text")
                return;

            var newMessage = new Message
            {
                Id = Guid.NewGuid(),
                ChannelId = SelectedChannel.Id,
                AuthorId = currentUser.Id,
                Content = MessageInput.Trim(),
                SentAt = DateTime.UtcNow
            };

            _db.Messages.Add(newMessage);
            await _db.SaveChangesAsync();

            Messages.Add(newMessage);
            MessageInput = string.Empty;
        }

        [RelayCommand]
        private async Task ToggleInviteManagerAsync()
        {
            if (!currentUser.IsAdmin)
            {
                MessageBox.Show("Admin access required.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!IsInviteManagerOpen)
            {
                await LoadInviteTokensAsync();
                IsInviteManagerOpen = true;
            }
            else
            {
                IsInviteManagerOpen = false;
            }
        }

        [RelayCommand]
        private async Task CreateInviteTokenAsync()
        {
            if (!currentUser.IsAdmin)
            {
                MessageBox.Show("Admin access required.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int maxUses = ParseIntOrDefault(InviteMaxUsesInput, 1, 1);
            int expiryDays = ParseIntOrDefault(InviteExpiryDaysInput, 7, 0);
            DateTime? expiresAt = expiryDays > 0 ? DateTime.UtcNow.AddDays(expiryDays) : null;

            var token = new InviteToken
            {
                Id = Guid.NewGuid(),
                Token = GenerateSecureInviteToken(),
                CreatedBy = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                MaxUses = maxUses,
                ExpiresAt = expiresAt,
                IsUsed = false
            };

            _db.InviteTokens.Add(token);
            await _db.SaveChangesAsync();

            InviteMaxUsesInput = maxUses.ToString();
            InviteExpiryDaysInput = expiryDays > 0 ? expiryDays.ToString() : "0";

            await LoadInviteTokensAsync();
        }

        [RelayCommand]
        private async Task RevokeInviteTokenAsync(InviteToken? token)
        {
            if (token == null)
                return;

            if (!currentUser.IsAdmin)
            {
                MessageBox.Show("Admin access required.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var entity = await _db.InviteTokens.FirstOrDefaultAsync(t => t.Id == token.Id);
            if (entity == null)
            {
                MessageBox.Show("Invite token not found.", "Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadInviteTokensAsync();
                return;
            }

            if (entity.IsUsed)
            {
                MessageBox.Show("Invite token is already used or revoked.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            entity.IsUsed = true;
            entity.ExpiresAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await LoadInviteTokensAsync();
        }

        private async Task LoadInviteTokensAsync()
        {
            var items = await _db.InviteTokens
                .OrderByDescending(t => t.CreatedAt)
                .Take(100)
                .ToListAsync();

            InviteTokens.Clear();
            foreach (var token in items)
            {
                InviteTokens.Add(token);
            }
        }

        private static string GenerateSecureInviteToken(int length = 20)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> chars = stackalloc char[length];
            Span<byte> random = stackalloc byte[length];
            RandomNumberGenerator.Fill(random);
            for (int i = 0; i < length; i++)
            {
                chars[i] = alphabet[random[i] % alphabet.Length];
            }

            return new string(chars);
        }

        private static int ParseIntOrDefault(string? input, int fallback, int minValue)
        {
            if (int.TryParse(input, out var parsed) && parsed >= minValue)
            {
                return parsed;
            }

            return fallback;
        }

        // Optional: Select server via command from sidebar buttons
        [RelayCommand]
        private void SelectServer(Server server)
        {
            _pendingChannelSelection = SelectedChannel?.Id;
            SelectedServer = server;
        }

        // Optional: Select channel
        [RelayCommand]
        private void SelectChannel(Channel channel)
        {
            SelectedChannel = channel;
        }

        [RelayCommand]
        private async Task DeleteServerAsync()
        {
            if (SelectedServer == null)
            {
                MessageBox.Show("Select a server first.", "No Server", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show(
                $"Delete server '{SelectedServer.Name}'? All channels and messages will be removed.",
                "Delete Server",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            var serverEntity = await _db.Servers.FirstOrDefaultAsync(s => s.Id == SelectedServer.Id);
            if (serverEntity == null)
            {
                MessageBox.Show("Server no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadServersAsync();
                return;
            }

            _db.Servers.Remove(serverEntity);
            await _db.SaveChangesAsync();

            await LoadServersAsync();
        }

        [RelayCommand]
        private async Task DeleteChannelAsync()
        {
            if (SelectedServer == null || SelectedChannel == null)
            {
                MessageBox.Show("Select a channel first.", "No Channel", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show(
                $"Delete channel '{SelectedChannel.Name}'? Messages inside it will be removed.",
                "Delete Channel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            var channelEntity = await _db.Channels.FirstOrDefaultAsync(c => c.Id == SelectedChannel.Id);
            if (channelEntity == null)
            {
                MessageBox.Show("Channel no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadChannelsAsync(SelectedServer.Id);
                return;
            }

            _db.Channels.Remove(channelEntity);
            await _db.SaveChangesAsync();

            await LoadChannelsAsync(SelectedServer.Id);
        }

        private void UpdateServerSettingsSnapshot(Server server)
        {
            ServerSettingsName = server.Name;
            ServerSettingsInviteCode = GenerateInviteCode(server.Id);
        }

        private static string GenerateInviteCode(Guid serverId)
        {
            var compact = serverId.ToString("N").ToUpperInvariant();
            return compact.Substring(0, Math.Min(8, compact.Length));
        }

        private Task ReloadServersPreservingSelectionAsync(Guid serverId)
        {
            SelectedServer = null;
            return PopulateServersAsync(serverId);
        }

        [RelayCommand]
        private void ToggleServerSettings()
        {
            if (SelectedServer == null)
            {
                MessageBox.Show("Select a server first.", "No Server", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!IsServerSettingsOpen)
            {
                UpdateServerSettingsSnapshot(SelectedServer);
            }

            IsServerSettingsOpen = !IsServerSettingsOpen;
        }

        [RelayCommand]
        private void CloseServerSettings()
        {
            IsServerSettingsOpen = false;
        }

        [RelayCommand]
        private async Task SaveServerSettingsAsync()
        {
            if (SelectedServer == null)
            {
                MessageBox.Show("Select a server first.", "No Server", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(ServerSettingsName))
            {
                MessageBox.Show("Server name cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string trimmedName = ServerSettingsName.Trim();
            if (trimmedName.Equals(SelectedServer.Name, StringComparison.Ordinal))
            {
                IsServerSettingsOpen = false;
                return;
            }

            var serverEntity = await _db.Servers.FirstOrDefaultAsync(s => s.Id == SelectedServer.Id);
            if (serverEntity == null)
            {
                MessageBox.Show("Server no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadServersAsync();
                return;
            }

            serverEntity.Name = trimmedName;
            await _db.SaveChangesAsync();

            _pendingChannelSelection = SelectedChannel?.Id;
            await ReloadServersPreservingSelectionAsync(serverEntity.Id);
            IsServerSettingsOpen = false;
        }

        [RelayCommand]
        private void ToggleUserSettings()
        {
            if (!IsUserSettingsOpen)
            {
                UserSettingsUsername = currentUser.Username;
                UserSettingsProfilePicturePath = currentUser.ProfilePicture;
            }

            IsUserSettingsOpen = !IsUserSettingsOpen;
        }

        [RelayCommand]
        private void CloseUserSettings()
        {
            IsUserSettingsOpen = false;
        }

        [RelayCommand]
        private async Task SaveUserSettingsAsync()
        {
            if (string.IsNullOrWhiteSpace(UserSettingsUsername))
            {
                MessageBox.Show("Username cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string trimmed = UserSettingsUsername.Trim();
            if (trimmed.Equals(currentUser.Username, StringComparison.Ordinal))
            {
                IsUserSettingsOpen = false;
                return;
            }

            var userEntity = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id);
            if (userEntity == null)
            {
                MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool duplicate = await _db.Users.AnyAsync(u => u.Id != currentUser.Id && u.Username.ToLower() == trimmed.ToLower());
            if (duplicate)
            {
                MessageBox.Show("Username already exists.", "Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            userEntity.Username = trimmed;
            currentUser.Username = trimmed;
            await _db.SaveChangesAsync();

            UserSettingsUsername = trimmed;
            OnPropertyChanged(nameof(CurrentUser));
            IsUserSettingsOpen = false;
        }

        [RelayCommand]
        private async Task UpdateUserProfilePictureAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp",
                Title = "Select Profile Picture"
            };

            bool? result = dialog.ShowDialog();
            if (result != true)
                return;

            var userEntity = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id);
            if (userEntity == null)
            {
                MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfilePictures");
            Directory.CreateDirectory(profilesDirectory);

            string extension = Path.GetExtension(dialog.FileName);
            string fileName = $"{currentUser.Id}{extension}";
            string destinationPath = Path.Combine(profilesDirectory, fileName);
            File.Copy(dialog.FileName, destinationPath, true);

            string relativePath = Path.Combine("ProfilePictures", fileName).Replace("\\", "/");
            userEntity.ProfilePicture = relativePath;
            currentUser.ProfilePicture = relativePath;
            await _db.SaveChangesAsync();

            UserSettingsProfilePicturePath = relativePath;
            OnPropertyChanged(nameof(CurrentUser));
        }

        [RelayCommand]
        private Task LogoutAsync()
        {
            Servers.Clear();
            Channels.Clear();
            Messages.Clear();
            InviteTokens.Clear();
            SelectedServer = null;
            SelectedChannel = null;
            MessageInput = string.Empty;
            IsUserSettingsOpen = false;
            IsInviteManagerOpen = false;
            MessageBox.Show("Logged out (placeholder until auth flow is built).", "Logout", MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        private bool copyNotificationVisible;
        public bool CopyNotificationVisible
        {
            get => copyNotificationVisible;
            set => SetProperty(ref copyNotificationVisible, value);
        }

        private string copyNotificationText = string.Empty;
        public string CopyNotificationText
        {
            get => copyNotificationText;
            set => SetProperty(ref copyNotificationText, value);
        }

        private bool copyNotificationIsError;
        public bool CopyNotificationIsError
        {
            get => copyNotificationIsError;
            set => SetProperty(ref copyNotificationIsError, value);
        }

        [RelayCommand]
        private async void CopyInviteToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            try
            {
                Clipboard.SetText(token);
                CopyNotificationText = "Token copied to clipboard";
                CopyNotificationIsError = false;
            }
            catch
            {
                CopyNotificationText = "Failed to copy token";
                CopyNotificationIsError = true;
            }

            CopyNotificationVisible = true;
            await Task.Delay(1800);
            CopyNotificationVisible = false;
        }
    }
}