using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

static ClientCommand ParseCommand(string receivedText)
{
    HashSet<string> CommandNames = new HashSet<string>(
        Enum.GetNames(typeof(ClientCommand)),
        StringComparer.OrdinalIgnoreCase
    );

    var matchedCommand = CommandNames.FirstOrDefault(command =>
        receivedText.StartsWith(command, StringComparison.OrdinalIgnoreCase)
    );

    return
        matchedCommand != null
        && Enum.TryParse(matchedCommand, ignoreCase: true, out ClientCommand result)
        ? result
        : ClientCommand.Unknown;
}

static void HandleConnectionAsync(
    Socket socket,
    ConcurrentDictionary<string, (string Value, DateTime? Expiry)> storage
)
{
    try
    {
        var buffer = new byte[1024];

        while (socket.Connected)
        {
            int bytesRead = socket.Receive(buffer);

            if (bytesRead == 0)
                break;

            string receivedText = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            var splitText = receivedText.Split("\r\n");
            var commandString = splitText[2];

            ClientCommand command = ParseCommand(commandString);

            switch (command)
            {
                case ClientCommand.Ping:
                    socket.Send(Encoding.ASCII.GetBytes("+PONG\r\n"));
                    break;
                case ClientCommand.Echo:
                    var echoStr = splitText[4];
                    var newStr = $"${echoStr.Length}\r\n{echoStr}\r\n";
                    socket.Send(Encoding.ASCII.GetBytes(newStr));
                    break;
                case ClientCommand.Set:
                    var newKey = splitText[4];
                    var newValue = splitText[6];

                    DateTime? expiry = null;
                    if (splitText.Length > 8 && (splitText[8] == "px"))
                    {
                        if (int.TryParse(splitText[10], out int expiryTime))
                        {
                            expiry = DateTime.UtcNow.AddMilliseconds(expiryTime);
                        }
                    }

                    var isSuccesful = storage.TryAdd(newKey, (newValue, expiry));
                    if (isSuccesful)
                    {
                        socket.Send(Encoding.ASCII.GetBytes("+OK\r\n"));
                    }
                    break;
                case ClientCommand.Get:
                    var key = splitText[4];
                    if (storage.TryGetValue(key, out var storedValue))
                    {
                        Console.WriteLine(key + ": " + storedValue);
                        if (
                            storedValue.Expiry.HasValue
                            && storedValue.Expiry.Value <= DateTime.UtcNow
                        )
                        {
                            storage.TryRemove(key, out _);
                            socket.Send(Encoding.ASCII.GetBytes("$-1\r\n"));
                            break;
                        }
                        else
                        {
                            var response =
                                $"${storedValue.Value.Length}\r\n{storedValue.Value}\r\n";
                            socket.Send(Encoding.ASCII.GetBytes(response));
                            break;
                        }
                    }
                    socket.Send(Encoding.ASCII.GetBytes("$-1\r\n"));
                    break;
                default:
                    throw new Exception("Uknown command");
            }
        }
    }
    catch (SocketException e)
    {
        Console.WriteLine($"SocketException: {e.Message}");
    }
    finally
    {
        socket.Close();
    }
}

TcpListener server = new TcpListener(IPAddress.Any, 6379);

try
{
    server.Start();

    var storage = new ConcurrentDictionary<string, (string Value, DateTime? Expiry)>();

    while (true)
    {
        Console.WriteLine("Waiting for a connection");

        var clientSocket = server.AcceptSocket();

        Thread clientThread = new Thread(() => HandleConnectionAsync(clientSocket, storage));
        clientThread.Start();
    }
}
catch (SocketException e)
{
    Console.WriteLine(e.ToString());
}
finally
{
    server.Stop();
}
