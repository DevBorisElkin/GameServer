﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatabaseAccess;

namespace GameServer
{
    public static class DatabaseBridge
    {
        public static void InitDatabase()
        {
            DatabaseManager.Connect();
        }

        public static async void TryToAuthenticateAsync(string _login, string _password)
        {
            UserData userData = new UserData { login = _login, password = _password };
            Task<UserData> task = Task<UserData>.Factory.StartNew(DatabaseManager.TryToAuthenticate, userData);
            await task;

            UserData result = task.Result;

            if (result.requestResult.Equals(RequestResult.Success))
            {
                //....
                // here we tell the user back that authentication succeeded, and give back UserData that he retrieved 
            }
            else
            {
                //....
                // here we tell the user back that authentication failed and give some clue why it did
            }
        }
    }
}
