using System;
using System.Collections.Generic;
using System.Text;

namespace client.Models;

public class Message
{
    public string Text { get; set; } = string.Empty;
    public bool IsOwnMessage { get; set; }
}
