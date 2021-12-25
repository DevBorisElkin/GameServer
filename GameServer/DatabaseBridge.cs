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
        static async void CheckDBConnection()
        {
            while (true)
            {
                Thread.Sleep((int)(TimeSpan.FromMinutes(checkDBConnectionMinutes).TotalMilliseconds));
                await CheckAndRebootIfNeeded();
            }
        }

        static UserData userDataCheckDBConnection = new UserData { login = "Bob_EA", password = "abobon" };
        static async Task CheckAndRebootIfNeeded()
        {
            UserData result = await DatabaseManager.TryToAuthenticateAsync(userDataCheckDBConnection);

            if (!result.requestResult.Equals(RequestResult.Success))
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
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to authenticate and got accepted");
            }
            else
            {
                // here we tell the user back that authentication failed and give some clue why it did
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to authenticate and was rejected [{result.requestResult}]");
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
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to register and got accepted");
            }
            else
            {
                // here we tell the user back that registration failed and give some clue why it did
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: client [{SendResponseBackTo.ch.ip}] requested to register and got rejected. reason:[{result.requestResult}]");
                SendMessageToClient($"{REGISTER_RESULT}|{result.requestResult}", SendResponseBackTo.ch);
            }
        }
        public static async Task<UserData> TryToChangeNicknameAsync(UserData proposedUserData)
        {
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToChangeNicknameAsync, proposedUserData);
            await task;
            return task.Result;
        }
        public static async void TryToWriteFullUserDataToConsole(int _dbId)
        {
            UserData userData = new UserData { db_id = _dbId, dataRequestType = DataRequestType.id };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToGetUserDataByDataRequestType, userData);
            await task;

            UserData result = task.Result;

            if (result.requestResult.Equals(RequestResult.Success))
            {
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: found user data by given db_id[{_dbId}]: {result.ToString()}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: couldn't find user data by given db_id[{_dbId}]:. reason:[{result.requestResult}]");
            }
        }

        public static async Task<UserData> GetUserData(int _dbId)
        {
            UserData userData = new UserData { db_id = _dbId, dataRequestType = DataRequestType.id };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToGetUserDataByDataRequestType, userData);
            await task;

            if (task.Result.requestResult.Equals(RequestResult.Success))
                return task.Result;
            else return null;
        }

        // this one is a bit different and returns RequestResult - is used for checking if user's account is in use
        public static async Task<UserData> GetUserData(string _login)
        {
            UserData userData = new UserData { login = _login, dataRequestType = DataRequestType.login };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToGetUserDataByDataRequestType, userData);
            await task;

            if (task.Result.requestResult.Equals(RequestResult.Success))
                return task.Result;
            else return task.Result;
        }
        //TryToUpdateUserData

        public static async Task<UserData> UpdateUserData(UserData newData)
        {
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToUpdateUserData, newData);
            await task;

            if (task.Result.requestResult.Equals(RequestResult.Success))
                return task.Result;
            else return null;
        }
    }
}
