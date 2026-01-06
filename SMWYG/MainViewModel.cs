using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.AspNetCore.SignalR.Client;
using SMWYG.Dialogs;
using SMWYG.Models;
using SMWYG.Services;
using SMWYG.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SMWYG.DTOs;

namespace SMWYG
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IApiService _api;
        private readonly IConfiguration _config;
        private HubConnection? _hubConnection;

        private Guid? _pendingChannelSelection;

        private readonly DispatcherTimer messageRefreshTimer;
        private bool isMessageRefreshTickRunning;
        private Guid? activeMessageRefreshChannelId;
        private DateTime latestMessageTimestampUtc = DateTime.MinValue;

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

        private ObservableCollection<User> adminUsers = new();
        public ObservableCollection<User> AdminUsers
        {
            get => adminUsers;
            set => SetProperty(ref adminUsers, value);
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

        // New user inputs
        private string adminNewUsername = string.Empty;
        public string AdminNewUsername
        {
            get => adminNewUsername;
            set => SetProperty(ref adminNewUsername, value);
        }

        private string adminNewPassword = string.Empty;
        public string AdminNewPassword
        {
            get => adminNewPassword;
            set => SetProperty(ref adminNewPassword, value);
        }

        private bool adminNewIsAdmin = false;
        public bool AdminNewIsAdmin
        {
            get => adminNewIsAdmin;
            set => SetProperty(ref adminNewIsAdmin, value);
        }

        // Current user (placeholder until real auth)
        private User currentUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Placeholder
            Username = "Adhil",
            IsAdmin = true
        };

        public User CurrentUser => currentUser;

        // New observable properties
        [ObservableProperty]
        private double uploadProgress;

        [ObservableProperty]
        private bool isUploading;

        [ObservableProperty]
        private long uploadSizeLimitBytes = 5 * 1024 * 1024; // default 5MB

        public MainViewModel(IApiService api, IConfiguration config)
        {
            _api = api;
            _config = config;

            // read upload limit from config
            var limitMb = config.GetValue<long?>("AppSettings:UploadSizeLimitMB");
            if (limitMb.HasValue && limitMb.Value > 0)
            {
                UploadSizeLimitBytes = limitMb.Value * 1024 * 1024;
            }

            UserSettingsUsername = currentUser.Username;
            UserSettingsProfilePicturePath = currentUser.ProfilePicture;

            messageRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            messageRefreshTimer.Tick += MessageRefreshTimer_Tick;

            InitializeSignalR();
        }

        [ObservableProperty]
        private bool isHubConnected;

        [ObservableProperty]
        private string hubStatus = "Disconnected";

        private void InitializeSignalR()
        {
            var apiBase = _config.GetValue<string>("ApiBaseUrl") ?? _config.GetValue<string>("AppSettings:ApiBaseUrl") ?? "https://localhost:5001/";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(new Uri(apiBase), "/hubs/chat"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Reconnecting += async (ex) =>
            {
                IsHubConnected = false;
                HubStatus = "Reconnecting...";
                await Task.CompletedTask;
            };

            _hubConnection.Reconnected += async (id) =>
            {
                IsHubConnected = true;
                HubStatus = "Connected";
                await Task.CompletedTask;
            };

            _hubConnection.Closed += async (ex) =>
            {
                IsHubConnected = false;
                HubStatus = "Disconnected";
                // try to restart after a short delay
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await _hubConnection.StartAsync();
                    IsHubConnected = true;
                    HubStatus = "Connected";
                }
                catch
                {
                    // swallow, next reconnect attempt will trigger
                }
            };

            _hubConnection.On<SMWYG.DTOs.MessageDto>("NewMessage", (dto) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var author = dto.Author;
                    if (author == null && dto.AuthorId == currentUser.Id)
                    {
                        author = currentUser;
                    }

                    var m = new Message
                    {
                        Id = dto.Id,
                        ChannelId = dto.ChannelId,
                        AuthorId = dto.AuthorId,
                        Author = author,
                        Content = dto.Content ?? string.Empty,
                        AttachmentUrl = dto.AttachmentUrl,
                        AttachmentContentType = dto.AttachmentContentType,
                        SentAt = dto.SentAt
                    };
                    Messages.Add(m);
                });
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await _hubConnection.StartAsync();
                    IsHubConnected = true;
                    HubStatus = "Connected";
                }
                catch
                {
                    IsHubConnected = false;
                    HubStatus = "Disconnected";
                }
            });
        }

        private async Task JoinHubChannelAsync(Guid channelId)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("JoinChannel", channelId.ToString());
        }

        private async Task LeaveHubChannelAsync(Guid channelId)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("LeaveChannel", channelId.ToString());
        }

        // Called after a successful login to populate current user and update bindings
        public void SignIn(User user)
        {
            if (user == null) return;
            currentUser = user;
            UserSettingsUsername = currentUser.Username;
            UserSettingsProfilePicturePath = currentUser.ProfilePicture;
            OnPropertyChanged(nameof(CurrentUser));
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
            var userServers = await _api.GetUserServersAsync(currentUser.Id);

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
                StopMessageRefresh();
                Messages.Clear();
            }
        }

        private async Task LoadChannelsAsync(Guid serverId, Guid? channelToSelect = null)
        {
            var serverChannels = await _api.GetChannelsAsync(serverId);

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
                StopMessageRefresh();
                Messages.Clear();
            }
        }

        // When a channel is selected → load messages if it's a text channel
        partial void OnSelectedChannelChanged(Channel? value)
        {
            if (value != null && value.Type == "text")
            {
                _ = LoadMessagesAsync(value.Id, true);
                _ = JoinHubChannelAsync(value.Id);
            }
            else
            {
                if (SelectedChannel != null)
                {
                    _ = LeaveHubChannelAsync(SelectedChannel.Id);
                }
                StopMessageRefresh();
                Messages.Clear();
            }
        }

        private async Task LoadMessagesAsync(Guid channelId, bool resetPollingState = false)
        {
            if (resetPollingState)
            {
                latestMessageTimestampUtc = DateTime.MinValue;
            }

            var channelMessages = await _api.GetMessagesAsync(channelId);

            Messages.Clear();
            foreach (var msg in channelMessages)
            {
                Messages.Add(msg);
            }

            if (channelMessages.Count > 0)
            {
                latestMessageTimestampUtc = channelMessages[^1].SentAt;
            }

            StartMessageRefresh(channelId);
            // Scroll to bottom (you can trigger this via event or behavior later)
        }

        // Create a new server
        [RelayCommand]
        private async Task CreateServerAsync()
        {
            var dialog = new TextPromptDialog("Create Server", "Server name", "Create", "My Awesome Server")
            {
                Owner = Application.Current?.MainWindow
            };

            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
                return;

            string name = dialog.InputText;
            if (string.IsNullOrWhiteSpace(name))
                return;

            var newServer = await _api.CreateServerAsync(name.Trim(), currentUser.Id);
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

            var serverEntity = await _api.GetUserServersAsync(currentUser.Id);
            // Note: API update icon flow expects server update endpoint; this is simplified.
            string iconsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerIcons");
            Directory.CreateDirectory(iconsDirectory);

            string extension = Path.GetExtension(dialog.FileName);
            // choose the selected server entity from returned list
            var server = serverEntity.FirstOrDefault(s => s.Id == SelectedServer.Id);
            if (server == null)
            {
                MessageBox.Show("Server no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadServersAsync();
                return;
            }

            string fileName = $"{server.Id}{extension}";
            string destinationPath = Path.Combine(iconsDirectory, fileName);
            File.Copy(dialog.FileName, destinationPath, true);

            string relativePath = Path.Combine("ServerIcons", fileName).Replace("\\", "/");
            await _api.UpdateServerIconAsync(server.Id, relativePath);

            _pendingChannelSelection = SelectedChannel?.Id;
            await ReloadServersPreservingSelectionAsync(server.Id);
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

            var channels = await _api.GetChannelsAsync(SelectedServer.Id);
            bool nameExists = channels.Any(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            if (nameExists)
            {
                MessageBox.Show("Channel name already exists for this server.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var created = await _api.CreateChannelAsync(SelectedServer.Id, channelName, channelType, null, channels.Count);
            await LoadChannelsAsync(SelectedServer.Id, created.Id);
        }

        [RelayCommand]
        private async Task RenameChannelAsync()
        {
            if (SelectedServer == null || SelectedChannel == null)
            {
                MessageBox.Show("Select a channel first.", "No Channel", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new TextPromptDialog("Rename Channel", "Channel name", "Save", SelectedChannel.Name)
            {
                Owner = Application.Current?.MainWindow
            };

            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
                return;

            string newName = dialog.InputText;
            if (string.IsNullOrWhiteSpace(newName))
                return;

            newName = newName.Trim();
            if (newName.Equals(SelectedChannel.Name, StringComparison.Ordinal))
                return;

            var channels = await _api.GetChannelsAsync(SelectedServer.Id);
            bool duplicate = channels.Any(c => c.Id != SelectedChannel.Id && c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                MessageBox.Show("Channel name already exists for this server.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var channelEntity = (await _api.GetChannelsAsync(SelectedServer.Id)).FirstOrDefault(c => c.Id == SelectedChannel.Id);
            if (channelEntity == null)
            {
                MessageBox.Show("Channel no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadChannelsAsync(SelectedServer.Id);
                return;
            }

            await _api.RenameChannelAsync(channelEntity.Id, newName);

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

            var created = await _api.SendMessageAsync(newMessage);
            // if SignalR is connected server will push to clients including this one; still add if not
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                Messages.Add(created);
            }

            if (created.SentAt > latestMessageTimestampUtc)
            {
                latestMessageTimestampUtc = created.SentAt;
            }
            MessageInput = string.Empty;
        }

        [RelayCommand]
        private async Task UploadAndSendAsync()
        {
            if (SelectedChannel == null || SelectedChannel.Type != "text")
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp",
                Title = "Select Image/GIF"
            };

            bool? result = dialog.ShowDialog();
            if (result != true) return;

            var filePath = dialog.FileName;
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > UploadSizeLimitBytes)
            {
                MessageBox.Show($"File too large. Limit is {UploadSizeLimitBytes / (1024 * 1024)} MB.", "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var uploadWindow = new SMWYG.Windows.UploadProgressWindow
            {
                Owner = Application.Current?.MainWindow
            };

            // bind progress
            uploadWindow.UploadProgress = UploadProgress;
            uploadWindow.IsUploading = true;

            uploadWindow.Show();

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var progressStream = new ProgressableStream(fs, (sent, total) =>
                {
                    UploadProgress = total > 0 ? (double)sent / total * 100.0 : 0;
                    // update modal window
                    uploadWindow.UploadProgress = UploadProgress;
                });

                var cancellationToken = uploadWindow.Cts.Token;
                var uploadResult = await _api.UploadFileAsync(progressStream, Path.GetFileName(filePath), MimeTypes.GetMimeType(filePath), cancellationToken);
                if (uploadResult == null)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await ShowNotification("Upload cancelled.", false);
                    }
                    else
                    {
                        await ShowNotification("Upload failed.", true);
                    }
                    return;
                }

                var msgDto = new MessageDto
                {
                    ChannelId = SelectedChannel.Id,
                    AuthorId = currentUser.Id,
                    Content = MessageInput?.Trim() ?? string.Empty,
                    AttachmentUrl = uploadResult.Url,
                    AttachmentContentType = uploadResult.ContentType,
                    SentAt = DateTime.UtcNow
                };

                var created = await _api.SendMessageAsync(msgDto);
                if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
                    Messages.Add(created);

                MessageInput = string.Empty;
            }
            finally
            {
                uploadWindow.Close();
                IsUploading = false;
                UploadProgress = 0;
            }
        }

        [RelayCommand]
        private void OpenImageViewer(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var viewer = new SMWYG.Windows.ImageViewer();
            viewer.LoadFromUrl(url);
            viewer.Show();
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
                await LoadUsersAsync();
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

            await _api.CreateInviteTokenAsync(token);

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

            await _api.RevokeInviteTokenAsync(token.Id);
            await LoadInviteTokensAsync();
        }

        private async Task LoadInviteTokensAsync()
        {
            var items = await _api.GetInviteTokensAsync();
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
        private async Task MoveChannelUpAsync(Channel? channel)
        {
            if (channel == null)
                return;

            await ReorderChannelAsync(channel, -1);
        }

        [RelayCommand]
        private async Task MoveChannelDownAsync(Channel? channel)
        {
            if (channel == null)
                return;

            await ReorderChannelAsync(channel, 1);
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

            var channelEntity = await _api.GetChannelsAsync(SelectedServer.Id);
            if (channelEntity == null)
            {
                MessageBox.Show("Channel no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadChannelsAsync(SelectedServer.Id);
                return;
            }

            await _api.DeleteChannelAsync(SelectedChannel.Id);
            await NormalizeChannelPositionsAsync(SelectedServer.Id);

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

            var serverEntity = await _api.GetUserServersAsync(currentUser.Id);
            var server = serverEntity.FirstOrDefault(s => s.Id == SelectedServer.Id);
            if (server == null)
            {
                MessageBox.Show("Server no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadServersAsync();
                return;
            }

            server.Name = trimmedName;
            await _api.UpdateServerAsync(server);

            _pendingChannelSelection = SelectedChannel?.Id;
            await ReloadServersPreservingSelectionAsync(server.Id);
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

            var userEntity = await _api.GetUserByIdAsync(currentUser.Id);
            if (userEntity == null)
            {
                MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool duplicate = await _api.UserExistsAsync(trimmed, currentUser.Id);
            if (duplicate)
            {
                MessageBox.Show("Username already exists.", "Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            userEntity.Username = trimmed;
            currentUser.Username = trimmed;
            await _api.UpdateUserAsync(userEntity);

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

            var userEntity = await _api.GetUserByIdAsync(currentUser.Id);
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
            await _api.UpdateUserAsync(userEntity);

            UserSettingsProfilePicturePath = relativePath;
            OnPropertyChanged(nameof(CurrentUser));
        }

        private bool PromptForLogin()
        {
            var login = App.Services.GetRequiredService<LoginWindow>();
            bool? result = login.ShowDialog();
            if (result == true && login.SignedInUser != null)
            {
                SignIn(login.SignedInUser);
                return true;
            }

            return false;
        }

        [RelayCommand]
        private async Task LogoutAsync()
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
            Application.Current?.MainWindow?.Hide();

            bool loggedIn = PromptForLogin();
            if (loggedIn)
            {
                Application.Current?.MainWindow?.Show();
                CopyNotificationText = string.Empty;
                CopyNotificationIsError = false;
                CopyNotificationVisible = false;
                MessageBox.Show("Logged out and re-signed in.", "Logout", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Application.Current?.Shutdown();
            }
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

        private async Task ShowNotification(string text, bool isError)
        {
            CopyNotificationText = text;
            CopyNotificationIsError = isError;
            CopyNotificationVisible = true;
            await Task.Delay(1800);
            CopyNotificationVisible = false;
        }

        public Task NotifyAsync(string text, bool isError)
        {
            return ShowNotification(text, isError);
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

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            var users = await _api.GetAllUsersAsync();
            AdminUsers.Clear();
            foreach (var u in users)
            {
                AdminUsers.Add(u);
            }
        }

        [RelayCommand]
        private async Task CreateUserAsync()
        {
            if (string.IsNullOrWhiteSpace(AdminNewUsername))
            {
                await ShowNotification("Username cannot be empty.", true);
                return;
            }

            var users = await _api.GetAllUsersAsync();
            bool exists = users.Any(u => u.Username.Equals(AdminNewUsername.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                await ShowNotification("Username already exists.", true);
                return;
            }

            try
            {
                var created = await _api.CreateUserAsync(AdminNewUsername.Trim(), AdminNewPassword ?? string.Empty, AdminNewIsAdmin);
            }
            catch (Exception)
            {
                await ShowNotification("Failed to create user.", true);
                return;
            }

            AdminNewUsername = string.Empty;
            AdminNewPassword = string.Empty;
            AdminNewIsAdmin = false;

            await LoadUsersAsync();
            await ShowNotification("User created.", false);
        }

        [RelayCommand]
        private async Task DeactivateUserAsync(User? user)
        {
            if (!CanManageUser(user))
                return;

            await _api.DeactivateUserAsync(user!.Id);
            await LoadUsersAsync();
            await ShowNotification($"{user.Username} deactivated.", false);
        }

        [RelayCommand]
        private async Task ReactivateUserAsync(User? user)
        {
            if (!CanManageUser(user))
                return;

            var result = await _api.ReactivateUserAsync(user!.Id);
            if (result == null)
            {
                await ShowNotification("User not found or could not be reactivated.", true);
                return;
            }

            await LoadUsersAsync();
            MessageBox.Show($"User '{user.Username}' reactivated.\nTemporary password: {result.TemporaryPassword}",
                "User Reactivated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task DeleteUserAsync(User? user)
        {
            if (!CanManageUser(user))
                return;

            var confirmation = MessageBox.Show(
                $"Delete user '{user!.Username}'? All servers they own, memberships, and messages will be removed.",
                "Delete User",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            await _api.DeleteUserAsync(user!.Id);
            await LoadUsersAsync();
            await ShowNotification("User deleted.", false);
        }

        private async Task ReorderChannelAsync(Channel channel, int direction)
        {
            if (SelectedServer == null)
                return;

            var orderedChannels = await _api.GetChannelsAsync(SelectedServer.Id);

            int currentIndex = orderedChannels.FindIndex(c => c.Id == channel.Id);
            if (currentIndex < 0)
                return;

            int targetIndex = currentIndex + direction;
            if (targetIndex < 0 || targetIndex >= orderedChannels.Count)
                return;

            (orderedChannels[currentIndex], orderedChannels[targetIndex]) = (orderedChannels[targetIndex], orderedChannels[currentIndex]);
            NormalizeChannelPositions(orderedChannels);
            await _api.ReorderChannelsAsync(SelectedServer.Id, orderedChannels.Select(c => c.Id).ToArray());
        }

        private async Task NormalizeChannelPositionsAsync(Guid serverId)
        {
            var orderedChannels = await _api.GetChannelsAsync(serverId);

            NormalizeChannelPositions(orderedChannels);
            await _api.ReorderChannelsAsync(serverId, orderedChannels.Select(c => c.Id).ToArray());
        }

        private static void NormalizeChannelPositions(List<Channel> channels)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                channels[i].Position = i;
            }
        }

        private bool CanManageUser(User? user)
        {
            if (!currentUser.IsAdmin)
            {
                MessageBox.Show("Admin access required.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (user == null)
                return false;

            if (user.Id == currentUser.Id)
            {
                MessageBox.Show("You cannot modify your own account from this panel.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task JoinServerAsync()
        {
            var dialog = new TextPromptDialog("Join Server", "Enter invite code", "Join")
            {
                Owner = Application.Current?.MainWindow
            };

            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
                return;

            string? inviteCode = dialog.InputText?.Trim();
            if (string.IsNullOrWhiteSpace(inviteCode))
                return;

            inviteCode = inviteCode.ToUpperInvariant();

            var server = await FindServerByInviteCodeAsync(inviteCode);
            if (server == null)
            {
                MessageBox.Show("No server matches that invite code.", "Invalid Invite", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var members = await _api.GetServerMembersAsync(server.Id);
            if (members.Any(m => m.UserId == currentUser.Id))
            {
                MessageBox.Show("You are already a member of this server.", "Already Joined", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var systemUser = await EnsureSystemUserAsync();
            await _api.AddServerMemberAsync(server.Id, currentUser.Id, "member");
            await BroadcastJoinSystemMessageAsync(server.Id, currentUser.Username, systemUser.Id);

            await ReloadServersPreservingSelectionAsync(server.Id);

            MessageBox.Show($"Joined '{server.Name}'.", "Server Joined", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<Server?> FindServerByInviteCodeAsync(string inviteCode)
        {
            var normalized = inviteCode.Trim().ToUpperInvariant();
            var servers = await _api.GetAllServersAsync();
            return servers.FirstOrDefault(s => GenerateInviteCode(s.Id).Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<User> EnsureSystemUserAsync()
        {
            var systemUser = (await _api.GetAllUsersAsync()).FirstOrDefault(u => u.Username == "System");
            if (systemUser != null)
            {
                return systemUser;
            }

            systemUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "System",
                DisplayName = "System",
                PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
                CreatedAt = DateTime.UtcNow,
                IsAdmin = true
            };

            // create system user via API
            var created = await _api.CreateUserAsync(systemUser.Username, Guid.NewGuid().ToString(), true);
            return created;
        }

        private async Task BroadcastJoinSystemMessageAsync(Guid serverId, string username, Guid systemUserId)
        {
            var textChannelIds = (await _api.GetChannelsAsync(serverId)).Where(c => c.Type == "text").Select(c => c.Id).ToList();

            if (textChannelIds.Count == 0)
                return;

            var timestamp = DateTime.UtcNow;
            foreach (var channelId in textChannelIds)
            {
                await _api.SendMessageAsync(new Message
                {
                    Id = Guid.NewGuid(),
                    ChannelId = channelId,
                    AuthorId = systemUserId,
                    Content = $"{username} joined the server.",
                    SentAt = timestamp
                });
            }
        }

        private void StartMessageRefresh(Guid channelId)
        {
            if (SelectedChannel == null || SelectedChannel.Type != "text" || SelectedChannel.Id != channelId)
            {
                return;
            }

            activeMessageRefreshChannelId = channelId;
            if (!messageRefreshTimer.IsEnabled)
            {
                messageRefreshTimer.Start();
            }
        }

        private void StopMessageRefresh()
        {
            messageRefreshTimer.Stop();
            activeMessageRefreshChannelId = null;
            latestMessageTimestampUtc = DateTime.MinValue;
            isMessageRefreshTickRunning = false;
        }

        private async void MessageRefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (isMessageRefreshTickRunning)
                return;

            if (activeMessageRefreshChannelId == null || SelectedChannel == null || SelectedChannel.Id != activeMessageRefreshChannelId || SelectedChannel.Type != "text")
            {
                StopMessageRefresh();
                return;
            }

            isMessageRefreshTickRunning = true;
            try
            {
                await FetchNewMessagesAsync(activeMessageRefreshChannelId.Value);
            }
            finally
            {
                isMessageRefreshTickRunning = false;
            }
        }

        private async Task FetchNewMessagesAsync(Guid channelId)
        {
            var newMessages = await _api.GetNewMessagesAsync(channelId, latestMessageTimestampUtc);

            if (newMessages.Count == 0)
                return;

            foreach (var message in newMessages)
            {
                Messages.Add(message);
                if (message.SentAt > latestMessageTimestampUtc)
                {
                    latestMessageTimestampUtc = message.SentAt;
                }
            }
        }

        // ProgressableStream class to report upload progress
        internal class ProgressableStream : Stream
        {
            private readonly Stream _inner;
            private readonly Action<long, long> _progress;
            private long _position;

            public ProgressableStream(Stream inner, Action<long, long> progress)
            {
                _inner = inner;
                _progress = progress;
                _position = 0;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;

            public override long Position { get => _inner.Position; set => _inner.Position = value; }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = _inner.Read(buffer, offset, count);
                _position += read;
                _progress?.Invoke(_position, Length);
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
                _position += count;
                _progress?.Invoke(_position, Length);
            }
        }
    }
}