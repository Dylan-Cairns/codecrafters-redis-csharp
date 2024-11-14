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

static void HandleConnectionAsync(Socket socket, ConcurrentDictionary<string, object> storage)
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
                    var isSuccesful = storage.TryAdd(newKey, newValue);
                    if (isSuccesful)
                    {
                        socket.Send(Encoding.ASCII.GetBytes("+OK\r\n"));
                    }
                    break;
                case ClientCommand.Get:
                    var key = splitText[4];
                    if (storage.TryGetValue(key, out var storedValue))
                    {
                        var storedStr = storedValue.ToString();
                        var response = $"${storedStr?.Length}\r\n{storedStr}\r\n";
                        socket.Send(Encoding.ASCII.GetBytes(response));
                    }
                    else
                    {
                        socket.Send(Encoding.ASCII.GetBytes("$-1\r\n")); // Redis NIL response for non-existent key
                    }
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

    var storage = new ConcurrentDictionary<string, object>();

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
