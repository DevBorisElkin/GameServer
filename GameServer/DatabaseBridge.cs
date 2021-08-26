using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatabaseAccess;
using static ServerCore.Util_Server;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.ClientHandler;
using System.Threading;

namespace ServerCore
{
    public static class DatabaseBridge
    {
        public static int checkDBConnectionMinutes = 1;

        public static void InitDatabase()
        {
            DatabaseManager.Connect();
            StartDBConnectionCheck();
        }
        public static void StartDBConnectionCheck()
        {
            DatabaseConnectionCheckTask = new Task(CheckDBConnection);
            DatabaseConnectionCheckTask.Start();
        }
        static Task DatabaseConnectionCheckTask;
        static void CheckDBConnection()
        {
            while (true)
            {
                Thread.Sleep((int)(TimeSpan.FromMinutes(checkDBConnectionMinutes).TotalMilliseconds));
                CheckAndRebootIfNeeded();
            }
        }

        static void CheckAndRebootIfNeeded()
        {
            if (!DatabaseManager.IsConnected()) DatabaseManager.Reboot();
        }

        public static async Task TryToAuthenticateAsync(string _login, string _password, Client SendResponseBackTo)
        {
            UserData userData = new UserData { login = _login, password = _password };
            UserData result = await DatabaseManager.TryToAuthenticateAsync(userData);
            //Task<UserData>.Factory.StartNew(DatabaseManager.TryToAuthenticateAsync, userData);

            result.ip = SendResponseBackTo.ch.ip;

            if (result.requestResult.Equals(RequestResult.Success))
            {
                // here we tell the user back that authentication succeeded, and give back UserData that he retrieved
                SendMessageToClient($"{LOG_IN_RESULT}|{result.requestResult}|{result.ToNetworkString()}", SendResponseBackTo.ch);
                SendResponseBackTo.userData = result;
                SendResponseBackTo.clientAccessLevel = ClientAccessLevel.Authenticated;
                Console.WriteLine($"[SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to authenticate and got accepted");
            }
            else
            {
                // here we tell the user back that authentication failed and give some clue why it did
                Console.WriteLine($"[SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to authenticate and was rejected [{result.requestResult}]");
                SendMessageToClient($"{LOG_IN_RESULT}|{result.requestResult}", SendResponseBackTo.ch);
            }
        }
        public static async void TryToRegisterAsync(string _login, string _password, string _nickname, Client SendResponseBackTo)
        {
            UserData userData = new UserData { login = _login, password = _password, nickname = _nickname };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToRegisterAsync, userData);
            await task;

            UserData result = task.Result;
            result.ip = SendResponseBackTo.ch.ip;

            if (result.requestResult.Equals(RequestResult.Success))
            {
                // here we tell the user back that registration succeeded, and give back UserData that he retrieved
                SendMessageToClient($"{REGISTER_RESULT}|{result.requestResult}|{result.ToNetworkString()}", SendResponseBackTo.ch);
                SendResponseBackTo.userData = result;
                SendResponseBackTo.clientAccessLevel = ClientAccessLevel.Authenticated;
                Console.WriteLine($"[SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to register and got accepted");
            }
            else
            {
                // here we tell the user back that registration failed and give some clue why it did
                SendMessageToClient($"{REGISTER_RESULT}|{result.requestResult}", SendResponseBackTo.ch);
            }
        }
    }
}
