using client.Models;
using client.Models.Dtos;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;

namespace client.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<ChatItemViewModel> Chats { get; set; } = [];
    public ObservableCollection<Message> CurrentMessages { get; set; } = [];
    [Reactive] public ChatItemViewModel? SelectedChat { get; set; }
    [Reactive] public string InputText { get; set; } = string.Empty;

    public ReactiveCommand<Unit, Unit> AddChatCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        AddChatCommand = ReactiveCommand.Create(AddNewChat);
        ToggleConnectionCommand = ReactiveCommand.CreateFromTask(ToggleConnection);
        SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessage);

        this.WhenAnyValue(x => x.SelectedChat)
            .Subscribe(chat =>
            {
                CurrentMessages.Clear();
                if (chat?.Messages is { } msgs)
                    foreach (var msg in msgs)
                        CurrentMessages.Add(msg);
            });
    }

    private void AddNewChat()
    {
        Chats.Add(new ChatItemViewModel());
    }

    private async Task ToggleConnection()
    {
        if (SelectedChat == null) return;

        if (SelectedChat.IsConnected)
        {
            await SelectedChat.Service!.DisconnectAsync();
            SelectedChat.IsConnected = false;
        }
        else
        {
            var service = _serviceProvider.GetRequiredService<IChatClientService>();
            service.DisplayName = SelectedChat.UserName;
            SelectedChat.Service = service;

            try
            {
                await service.ConnectAsync(SelectedChat.Host, SelectedChat.Port, SelectedChat.UserName);
                SelectedChat.IsConnected = true;

                var disposable = Observable.FromEvent<Action<Message>, Message>(
                        h => service.OnMessageReceived += h,
                        h => service.OnMessageReceived -= h)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(msg =>
                    {
                        SelectedChat.Messages.Add(msg);
                        if (ReferenceEquals(SelectedChat, this.SelectedChat))
                            CurrentMessages.Add(msg);
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task SendMessage()
    {
        if (SelectedChat?.Service is not { } service || !service.IsConnected || string.IsNullOrWhiteSpace(InputText))
            return;

        var ownMsg = new Message
        {
            Author = SelectedChat.UserName,
            Text = InputText,
            Time = DateTime.Now,
            IsOwnMessage = true
        };

        SelectedChat.Messages.Add(ownMsg);
        if (ReferenceEquals(SelectedChat, this.SelectedChat))
            CurrentMessages.Add(ownMsg);

        var chatMsg = new ChatMessageDto(Author: SelectedChat.UserName, Text: InputText);

        try
        {
            await service.SendMessageAsync(chatMsg);
            InputText = string.Empty;
        }
        catch (Exception ex)
        {
            var errorMsg = new Message { Text = $"Ошибка: {ex.Message}", Time = DateTime.Now };
            SelectedChat.Messages.Add(errorMsg);
            if (ReferenceEquals(SelectedChat, SelectedChat))
                CurrentMessages.Add(errorMsg);
        }
    }
}