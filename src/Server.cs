using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");


TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
var clientSocket = server.AcceptSocket();

while (clientSocket.Connected)
{

    var buffer = new byte[1024];

    await clientSocket.ReceiveAsync(buffer);

    await clientSocket.SendAsync(Encoding.ASCII.GetBytes("+PONG\r\n"));

}
