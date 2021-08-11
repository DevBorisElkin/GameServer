using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatabaseAccess;
using static GameServer.Util_Server;
using static GameServer.NetworkingMessageAttributes;

namespace GameServer
{
    public static class DatabaseBridge
    {
        public static void InitDatabase()
        {
            DatabaseManager.Connect();
        }

        public static async void TryToAuthenticateAsync(string _login, string _password, ClientHandler SendResponseBackTo)
        {
            UserData userData = new UserData { login = _login, password = _password };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToAuthenticateAsync, userData);
            await task;

            UserData result = task.Result;
            result.ip = SendResponseBackTo.ip;

            if (result.requestResult.Equals(RequestResult.Success))
            {
                // here we tell the user back that authentication succeeded, and give back UserData that he retrieved
                SendMessageToClient($"{LOG_IN_RESULT}|{result.requestResult}|{result.ToNetworkString()}", SendResponseBackTo);
                SendResponseBackTo.userData = result;
                SendResponseBackTo.clientAccessLevel = ClientAccessLevel.Authenticated;
                Console.WriteLine($"[SERVER_MESSAGE]: client [{SendResponseBackTo.ip}] requested to authenticate and got accepted");
            }
            else
            {
                // here we tell the user back that authentication failed and give some clue why it did
                SendMessageToClient($"{LOG_IN_RESULT}|{result.requestResult}", SendResponseBackTo);
            }
        }
        public static async void TryToRegisterAsync(string _login, string _password, string _nickname, ClientHandler SendResponseBackTo)
        {
            UserData userData = new UserData { login = _login, password = _password, nickname = _nickname };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToRegisterAsync, userData);
            await task;

            UserData result = task.Result;
            result.ip = SendResponseBackTo.ip;

            if (result.requestResult.Equals(RequestResult.Success))
            {
                // here we tell the user back that registration succeeded, and give back UserData that he retrieved
                SendMessageToClient($"{REGISTER_RESULT}|{result.requestResult}|{result.ToNetworkString()}", SendResponseBackTo);
                SendResponseBackTo.userData = result;
                SendResponseBackTo.clientAccessLevel = ClientAccessLevel.Authenticated;
                Console.WriteLine($"[SERVER_MESSAGE]: client [{SendResponseBackTo.ip}] requested to register and got accepted");
            }
            else
            {
                // here we tell the user back that registration failed and give some clue why it did
                SendMessageToClient($"{REGISTER_RESULT}|{result.requestResult}", SendResponseBackTo);
            }
        }
    }
}
