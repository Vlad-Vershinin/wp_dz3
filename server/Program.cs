using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace server
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<TcpClient, string> _clientNicknames = new();

        static async Task Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Any, 8080);
            listener.Start();
            Console.WriteLine("Сервер запущен на порту 8080. Ожидание подключений...");

            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [CRITICAL ERROR] - {ex.Message}");
            }
            finally
            {
                listener.Stop();
                foreach (var client in _clientNicknames.Keys)
                {
                    client.Close();
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                var regMessage = await TcpMessageHelper.ReceiveChatMessage(client);

                if (string.IsNullOrWhiteSpace(regMessage.Author))
                    throw new InvalidDataException("Ник не может быть пустым");

                string nickname = regMessage.Author.Trim();
                _clientNicknames[client] = nickname;

                Console.WriteLine($"[JOIN] {nickname} подключился");

                var joinNotification = new ChatMessageDto("[SYSTEM]", $"{nickname} присоединился к чату");
                await BroadcastToAllAsync(joinNotification, exclude: client);

                while (client.Connected)
                {
                    var msg = await TcpMessageHelper.ReceiveChatMessage(client);

                    var actualNick = _clientNicknames.GetValueOrDefault(client, "unknown");
                    var finalMessage = new ChatMessageDto(actualNick, msg.Text);

                    Console.WriteLine($"[{actualNick}] {msg.Text}");
                    await BroadcastToAllAsync(finalMessage, exclude: client);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [ERROR] - {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                if (_clientNicknames.TryRemove(client, out var nick))
                {
                    Console.WriteLine($"[LEAVE] {nick} отключился");
                    var leaveMsg = new ChatMessageDto("[SYSTEM]", $"{nick} покинул чат");
                    await BroadcastToAllAsync(leaveMsg, exclude: null);
                }
                client.Close();
            }
        }

        private static async Task BroadcastToAllAsync(ChatMessageDto message, TcpClient? exclude = null)
        {
            var json = JsonSerializer.Serialize(message);
            foreach (var (client, _) in _clientNicknames)
            {
                if (client == exclude) continue;

                try
                {
                    await TcpMessageHelper.SendMessage(client, json);
                }
                catch
                {
                    _clientNicknames.TryRemove(client, out _);
                    client.Close();
                }
            }
        }

        //private static async Task BroadcastChatMessage(ChatMessageDto message, TcpClient? exclude = null)
        //{
        //    var json = JsonSerializer.Serialize(message);
        //    foreach (var (client, _) in _clientNicknames)
        //    {
        //        if (client == exclude) continue;

        //        try
        //        {
        //            await TcpMessageHelper.SendMessage(client, json);
        //        }
        //        catch
        //        {
        //            _clientNicknames.TryRemove(client, out _);
        //            client.Close();
        //        }
        //    }
        //}

        //private static async Task BroadcastSystemMessage(string systemText, TcpClient? exclude = null)
        //{
        //    var systemMsg = new ChatMessageDto("[SYSTEM]", systemText);
        //    await BroadcastChatMessage(systemMsg, exclude);
        //}
    }
}