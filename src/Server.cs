using System.Net;
using System.Net.Sockets;
using System.Text;

static ClientCommand ParseCommand(string receivedText)
{
    if (receivedText.StartsWith("PING", StringComparison.OrdinalIgnoreCase))
    {
        return ClientCommand.Ping;
    }
    else if (receivedText.StartsWith("ECHO", StringComparison.OrdinalIgnoreCase))
    {
        return ClientCommand.Echo;
    }
    else
    {
        return ClientCommand.Unknown;
    }
}

static void HandleConnectionAsync(Socket socket)
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

    while (true)
    {
        Console.WriteLine("Waiting for a connection");

        var clientSocket = server.AcceptSocket();

        Thread clientThread = new Thread(() => HandleConnectionAsync(clientSocket));
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
