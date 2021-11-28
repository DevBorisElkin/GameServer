using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseAccess
{
    public class UserData
    {
        // id = unickue database identifier of the user
        public int db_id;
        public string login;
        public string password;
        public string nickname;
        public string ip;
        public AccessRights accessRights;

        // stores result of request to Database
        public RequestResult requestResult;
        public DataRequestType dataRequestType;

        public UserData() { }
        public UserData(RequestResult requestResult)
        {
            this.requestResult = requestResult;
        }
        public UserData(int id, string login, string password, string nickname, AccessRights accessRights, RequestResult requestResult = RequestResult.Success)
        {
            this.db_id = id;
            this.login = login;
            this.password = password;
            this.nickname = nickname;
            this.accessRights = accessRights;
            this.requestResult = requestResult;
        }

        public override string ToString()
        {
            return $"id:[{db_id}], login:[{login}], password:[{password}], nickname:[{nickname}], ip:[{ip}], accessRights:[{accessRights}]";
        }
        public string ToNetworkString()
        {
            return $"{db_id},{login},{password},{nickname},{ip},{accessRights}";
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
        Fail_NicknameAlreadyTaken = 6 ,
        Fail_NoUserWithGivenLogin = 7
    }

    public enum AccessRights
    {
        User,
        Admin,
        SuperAdmin
    }

    public enum DataRequestType
    {
        id,
        login
    }
}
