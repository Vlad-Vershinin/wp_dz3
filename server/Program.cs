using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace server
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<TcpClient, byte> _clients = new();

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
                    _clients.TryAdd(client, 0);

                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} [CRITICAL ERROR] - {ex.Message}");
            }
            finally
            {
                listener.Stop();
                foreach (var client in _clients.Keys)
                {
                    client.Close();
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                while (client.Connected)
                {
                    string message = await TcpMessageHelper.ReceiveMessage(client);
                    Console.WriteLine($"Получено: {message}");
                    await BroadcastAsync(message, exclude: client);
                }
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} [INFO] - Клиент оключился: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} [ERROR] - {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                _clients.TryRemove(client, out _);
                client.Close();
            }
        }

        private static async Task BroadcastAsync(string message, TcpClient exclude)
        {
            foreach (var client in _clients.Keys)
            {
                if (client == exclude || !client.Connected)
                    continue;

                try
                {
                    await TcpMessageHelper.SendMessage(client, message);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} [Error] - {ex.Message}");
                    Console.ResetColor();
                    _clients.TryRemove(client, out _);
                    client.Close();
                }
            }
        }
    }
}