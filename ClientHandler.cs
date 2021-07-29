using GameServer;
using System;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class ClientHandler
{
    Server server;
    public Socket handler;

    public int id;
    public string ip;

    public Player player;

    Task taskListener;
    public ClientHandler(Server server, Socket handler, int id)
    {
        this.server = server;
        this.handler = handler;
        this.id = id;
        ip = this.GetRemoteIp();

        taskListener = new Task(ListenToMessages);
        taskListener.Start();
    }

    int errorMessages = 0;
    // [LISTEN TO MESSAGES]
    void ListenToMessages()
    {
        byte[] bytes = new byte[1024];
        string str;

        while (true)
        {
            try
            {
                str = ReadLine2(handler, bytes);
                if (!str.Equals(""))
                {
                    server.OnMessageReceived(str, this);
                }
                else
                {
                    errorMessages++;
                    if (errorMessages > 100)
                    {
                        ShutDownClient(1);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                ShutDownClient(2);
                break;
            }
        }
    }

    string ReadLine(Socket reciever, byte[] buffer)
    {
        int bytesRec = reciever.Receive(buffer);
        string data = Encoding.ASCII.GetString(buffer, 0, bytesRec);
        return data;
    }

    string ReadLine2(Socket reciever, byte[] buffer)
    {
        StringBuilder builder = new StringBuilder();
        int bytes = 0; // amount of received bytes
        do
        {
            bytes = reciever.Receive(buffer);
            builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
        }
        while (handler.Available > 0);

        return builder.ToString();
    }
    public void SendMessage(string message)
    {
        byte[] dataToSend = Encoding.Unicode.GetBytes(message);
        handler.Send(dataToSend);
    }

    public void ShutDownClient(int error = 0, bool removeFromClientsList = true)
    {
        server.OnClientDisconnected(this, error.ToString());
        handler.Dispose();
        if (removeFromClientsList) server.clients.Remove(this.id);
        taskListener.Dispose();
    }

    public void SendIntoGame(string userName)
    {
        player = new Player(id, userName, new Vector3(0,0,0));

        foreach(ClientHandler client in server.clients.Values)
        {
            if(client.player != null)
            {
                if(client.id != id)
                {

                }
            }
        }
    }
}

public class Player
{
    public int id;
    public string username;

    public Vector3 position;
    public Quaternion rotation;
    public Player(int _id, string _username, Vector3 _spawnPosition)
    {
        id = _id;
        username = _username;
        position = _spawnPosition;
        rotation = Quaternion.Identity;
    }
}