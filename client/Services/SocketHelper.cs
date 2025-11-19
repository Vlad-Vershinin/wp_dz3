using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace client.Services;

public static class TcpMessageHelper
{
    public static async Task SendMessage(TcpClient client, string message)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client), "TcpClient не может быть null");

        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Сообщение не может быть пустым", nameof(message));

        if (!client.Connected)
            throw new InvalidOperationException("Клиент отключён");

        Debug.WriteLine($"[Отправка] Подготовка сообщения: \"{message}\"");

        try
        {
            NetworkStream stream = client.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            Debug.WriteLine($"[Отправка] Размер сообщения в байтах: {messageBytes.Length}");

            byte[] lengthPrefix = BitConverter.GetBytes(messageBytes.Length);
            Debug.WriteLine($"[Отправка] Префикс длины (4 байта): {BitConverter.ToString(lengthPrefix)}");

            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            Debug.WriteLine($"[Отправка] Отправлено 4 байта длины");

            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            Debug.WriteLine($"[Отправка] Отправлено {messageBytes.Length} байт данных");
            Debug.WriteLine($"[Отправка] Сообщение успешно отправлено: \"{message}\"");
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"[ОШИБКА] Ошибка ввода-вывода при отправке: {ex.Message}");
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine($"[ОШИБКА] Соединение закрыто: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ОШИБКА] Неизвестная ошибка при отправке: {ex.Message}");
            throw;
        }
    }

    public static async Task<string> ReceiveMessage(TcpClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client), "TcpClient не может быть null");

        if (!client.Connected)
            throw new InvalidOperationException("Клиент отключён");

        Debug.WriteLine("[Получение] Начало получения сообщения...");

        try
        {
            NetworkStream stream = client.GetStream();

            byte[] lengthBuffer = new byte[4];
            int bytesRead = 0;

            Debug.WriteLine("[Получение] Ожидание получения 4 байт длины сообщения...");
            while (bytesRead < 4)
            {
                int result = await stream.ReadAsync(lengthBuffer, bytesRead, 4 - bytesRead);
                if (result == 0)
                {
                    Debug.WriteLine("[Получение] Соединение разорвано удалённой стороной");
                    throw new IOException("Соединение закрыто удалённой стороной");
                }

                bytesRead += result;
                Debug.WriteLine($"[Получение] Прочитано байт длины: {result} (всего: {bytesRead}/4)");
            }

            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            Debug.WriteLine($"[Получение] Получена длина сообщения: {messageLength} байт");

            if (messageLength <= 0 || messageLength > 10 * 1024 * 1024)
            {
                Debug.WriteLine($"[ОШИБКА] Некорректная длина сообщения: {messageLength} байт");
                throw new InvalidOperationException($"Некорректная длина сообщения: {messageLength}");
            }

            byte[] messageBuffer = new byte[messageLength];
            bytesRead = 0;

            Debug.WriteLine($"[Получение] Ожидание получения {messageLength} байт данных...");

            while (bytesRead < messageLength)
            {
                int result = await stream.ReadAsync(messageBuffer, bytesRead, messageLength - bytesRead);
                if (result == 0)
                {
                    Debug.WriteLine("[Получение] Соединение разорвано во время получения данных");
                    throw new IOException("Соединение закрыто во время чтения");
                }

                bytesRead += result;
                Debug.WriteLine($"[Получение] Прочитано байт данных: {result} (всего: {bytesRead}/{messageLength})");
            }

            string message = Encoding.UTF8.GetString(messageBuffer);
            Debug.WriteLine($"[Получение] Успешно получено сообщение: \"{message}\"");

            return message;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"[ОШИБКА] Ошибка ввода-вывода при получении: {ex.Message}");
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine($"[ОШИБКА] Поток или клиент закрыт: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ОШИБКА] Неизвестная ошибка при получении: {ex.Message}");
            throw;
        }
    }
}