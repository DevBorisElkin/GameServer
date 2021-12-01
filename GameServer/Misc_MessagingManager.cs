using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ServerCore.DataTypes;
using static ServerCore.NetworkingMessageAttributes;

namespace ServerCore
{
    public static class Misc_MessagingManager
    {
        public static void SendMessageToTheClient(string messageBody, ClientHandler client, MessageFromServer_WindowType windowType, MessageFromServer_MessageType messageType)
        {
            string message = $"{MESSAGE_FROM_SERVER}|{windowType}|{messageType}|{messageBody}";
            Util_Server.SendMessageToClient(message, client, Util_Server.MessageProtocol.TCP);
        }
    }
}
