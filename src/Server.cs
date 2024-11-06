using System.Net;
using System.Net.Sockets;
using System.Text;


static void HandleConnectionAsync(Socket socket)
{
    while (clientSocket.Connected)
    {

        var buffer = new byte[1024];

        await clientSocket.ReceiveAsync(buffer);

        await clientSocket.SendAsync(Encoding.ASCII.GetBytes("+PONG\r\n"));
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

        Threat clientThread = new Thread(() => HandleConnectionAsync(clientSocket)
    }
} catch (SocketException e)
{
    Console.WriteLine(e.ToString());
} finally
{
    server.Stop()    
}



