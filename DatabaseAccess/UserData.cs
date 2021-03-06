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

        //additional data
        public int total_games;
        public int total_victories;
        public int kills;
        public int deaths;
        public int runes_picked_up;

        // stores result of request to Database
        public RequestResult requestResult;
        public DataRequestType dataRequestType;

        public UserData() { }
        public UserData(RequestResult requestResult)
        {
            this.requestResult = requestResult;
        }
        public UserData(int id, string login, string password, string nickname, AccessRights accessRights, int totalGames, int totalVictories,
            int kills, int deaths, int runes_picked_up, RequestResult requestResult = RequestResult.Success)
        {                                                                                                  
            this.db_id = id;                                                                               
            this.login = login;                                                                            
            this.password = password;                                                                      
            this.nickname = nickname;
            this.accessRights = accessRights;
            this.requestResult = requestResult;
            this.total_games = totalGames;
            this.total_victories = totalVictories;
            this.kills = kills;
            this.deaths = deaths;
            this.runes_picked_up = runes_picked_up;
        }

        public UserData(UserData copyFrom) 
        {
            this.db_id = copyFrom.db_id;
            this.login = copyFrom.login;
            this.password = copyFrom.password;
            this.nickname = copyFrom.nickname;
            this.accessRights = copyFrom.accessRights;
            this.requestResult = copyFrom.requestResult;
            this.total_games = copyFrom.total_games;
            this.total_victories = copyFrom.total_victories;
            this.kills = copyFrom.kills;
            this.deaths = copyFrom.deaths;
            this.runes_picked_up = copyFrom.runes_picked_up;
        }

        public override string ToString()
        {
            return $"id:[{db_id}], login:[{login}], password:[{password}], nickname:[{nickname}], ip:[{ip}], accessRights:[{accessRights}]" +
                $", totalGames:[{total_games}], totalVictories:[{total_victories}], kills:[{kills}], deaths:[{deaths}], runesPickedUp:[{runes_picked_up}]";
        }
        public string ToNetworkString()
        {
            return $"{db_id},{login},{password},{nickname},{ip},{accessRights},{total_games},{total_victories},{kills},{deaths},{runes_picked_up}";
        }
        public string ToNetworkStringSecured()
        {
            return $"{db_id},login,password,{nickname},{ip},{accessRights},{total_games},{total_victories},{kills},{deaths},{runes_picked_up}";
        }

        public static bool HasValueChanged(UserData _old, UserData _new)
        {
            if (_old == null || _new == null) return false;
            if (_old.db_id != _new.db_id) return true;
            if(_old.login !=      _new.login) return true;
            if(_old.password !=       _new.password) return true;
            if (_old.nickname !=       _new.nickname) return true;
            if (_old.accessRights !=   _new.accessRights) return true;
            if (_old.requestResult !=  _new.requestResult) return true;
            if (_old.total_games !=    _new.total_games) return true;
            if (_old.total_victories !=_new.total_victories) return true;
            if (_old.kills !=          _new.kills) return true;
            if (_old.deaths !=         _new.deaths) return true;
            if (_old.runes_picked_up != _new.runes_picked_up) return true;
            return false;
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
