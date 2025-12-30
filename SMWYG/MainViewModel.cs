using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMWYG.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SMWYG
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        // Collections
        [ObservableProperty]
        private ObservableCollection<Server> servers = new();

        [ObservableProperty]
        private ObservableCollection<Channel> channels = new();

        [ObservableProperty]
        private ObservableCollection<Message> messages = new();

        // Selected items
        [ObservableProperty]
        private Server? selectedServer;

        [ObservableProperty]
        private Channel? selectedChannel;

        // Input
        [ObservableProperty]
        private string messageInput = string.Empty;

        // Current user (hardcoded for now – replace with real login later)
        private readonly User currentUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Placeholder
            Username = "Adhil"
        };

        public MainViewModel(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // Load all servers the current user is a member of
        [RelayCommand]
        public async Task LoadServersAsync()
        {
            var userServers = await _db.ServerMembers
                .Where(sm => sm.UserId == currentUser.Id)
                .Select(sm => sm.Server)
                .OrderBy(s => s.Name)
                .ToListAsync();

            Servers.Clear();
            foreach (var server in userServers)
            {
                Servers.Add(server);
            }

            // Auto-select first server if any
            if (Servers.Any() && SelectedServer == null)
            {
                SelectedServer = Servers.First();
            }
        }

        // When a server is selected → load its channels
        partial void OnSelectedServerChanged(Server? value)
        {
            if (value != null)
            {
                _ = LoadChannelsAsync(value.Id);
            }
            else
            {
                Channels.Clear();
                Messages.Clear();
            }
        }

        private async Task LoadChannelsAsync(Guid serverId)
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

            // Auto-select first text channel
            SelectedChannel = Channels.FirstOrDefault(c => c.Type == "text") ?? Channels.FirstOrDefault();
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

            Servers.Add(newServer);
            SelectedServer = newServer; // Auto-select the new server
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

        // Optional: Select server via command from sidebar buttons
        [RelayCommand]
        private void SelectServer(Server server)
        {
            SelectedServer = server;
        }

        // Optional: Select channel
        [RelayCommand]
        private void SelectChannel(Channel channel)
        {
            SelectedChannel = channel;
        }
    }
}