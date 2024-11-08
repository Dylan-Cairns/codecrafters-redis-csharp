using System.Net;
using System.Net.Sockets;
using System.Text;

static void HandleConnectionAsync(Socket socket)
{
    try
    {
        while (socket.Connected)
        {
            var buffer = new byte[1024];

            socket.ReceiveAsync(buffer);

            socket.SendAsync(Encoding.ASCII.GetBytes("+PONG\r\n"));
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
