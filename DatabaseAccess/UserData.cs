﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseAccess
{
    public class UserData
    {
        public int id;
        public string login;
        public string password;
        public string nickname;

        public RequestResult requestResult;

        public UserData() { }
        public UserData(RequestResult requestResult)
        {
            this.requestResult = requestResult;
        }
        public UserData(int id, string login, string password, string nickname, RequestResult requestResult = RequestResult.Success)
        {
            this.id = id;
            this.login = login;
            this.password = password;
            this.nickname = nickname;
            this.requestResult = requestResult;
        }

        public override string ToString()
        {
            return $"id:[{id}], login:[{login}], password:[{password}], nickname:[{nickname}]";
        }
    }
    // here I will populate different DatabaseRequestResults
    public enum RequestResult 
    { 
        Success = 0,
        Fail = 1, 
        Fail_NoConnectionToDB = 2,
        Fail_WrongPairLoginPassword = 3,
        Fail_LoginAlreadyTaken = 4 ,
        Fail_NoUserWithGivenLogin = 5
    }
}