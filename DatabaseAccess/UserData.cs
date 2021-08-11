using System;
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
        public string ip;

        // stores result of request to Database
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
            return $"id:[{id}], login:[{login}], password:[{password}], nickname:[{nickname}], ip:[{ip}]";
        }
        public string ToNetworkString()
        {
            return $"{id},{login},{password},{nickname},{ip}";
        }
    }
    // here I will populate different DatabaseRequestResults
    public enum RequestResult 
    { 
        None = 0,
        Success = 1,
        Fail = 2, 
        Fail_NoConnectionToDB = 3,
        Fail_WrongPairLoginPassword = 4,
        Fail_LoginAlreadyTaken = 5 ,
        Fail_NoUserWithGivenLogin = 6
    }
}
