using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ServerCore.Util_Server;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.ClientHandler;
using static ServerCore.PlayroomManager;
using static ServerCore.PlayroomManager_MapData;
using DatabaseAccess;

namespace ServerCore
{
    public class Client
    {
        public ClientHandler ch;
        public ClientAccessLevel clientAccessLevel;
        public UserData _userData;

        public Action userDataChanged;

        public UserData userData
        {
            get { return _userData; }
            set
            {
                bool valueChanged = UserData.HasValueChanged(_userData, value);
                _userData = value;
                if (valueChanged) userDataChanged?.Invoke();
            }
        }

        void NotifyClientOnDataChanged()
        {
            //Console.WriteLine($"[{DateTime.Now}][SYSTEM MESSAGE]: On user data changed [{userData.db_id}][{userData.nickname}]");
            Util_Server.SendMessageToClient($"{GET_USER_DATA_RESULT}|{userData.ToNetworkString()}", ch);
        }

        public Player player;
        public Client(ClientHandler _ch)
        {
            ch = _ch;
            clientAccessLevel = ClientAccessLevel.LowestLevel;
            userDataChanged += NotifyClientOnDataChanged;
        }


        public void DestroyClient()
        {
            ch = null;
            player = null;
            userDataChanged -= NotifyClientOnDataChanged;
            userData = null;
        }

        // addons
        public static List<Client> connected_clients;

        public static Client GetClientByClientHandler(ClientHandler ch)
        {
            foreach (Client a in connected_clients)
            {
                if (a.ch == ch)
                {
                    return a;
                }
            }
            return null;
        }
        public static void RemoveClient(ClientHandler ch)
        {
            foreach (Client a in connected_clients)
            {
                if (a.ch == ch)
                {
                    a.DestroyClient();
                    connected_clients.Remove(a);
                    break;
                }
            }
        }
        public async Task<UserData> RefreshUserDataFromDatabase()
        {
            UserData retrievedUserData = await DatabaseBridge.GetUserData(userData.db_id);
            if(retrievedUserData != null)
            {
                userData = retrievedUserData;
                return retrievedUserData;
            }
            return null;
        }
        public async Task<UserData> UpdateUserData_AccessRights(AccessRights ar)
        {
            AccessRights old = userData.accessRights;
            userData.accessRights = ar;
            UserData retrievedUserData = await DatabaseBridge.UpdateUserData(userData);
            if (retrievedUserData != null)
            {
                userData = retrievedUserData;
                return retrievedUserData;
            }
            else
            {
                userData.accessRights = old;
                return null;
            }
        }
        // Global
        public static async Task<UserData> GetUserDataFromDatabase(int db_id)
        {
            UserData retrievedUserData = await DatabaseBridge.GetUserData(db_id);
            if (retrievedUserData != null)
                return retrievedUserData;
            return null;
        }
        public static async Task<UserData> UpdateUserData(Client client, UserData updatedUserData)
        {
            UserData retrievedUserData = await DatabaseBridge.UpdateUserData(updatedUserData);
            if (retrievedUserData != null)
            {
                client.userData = retrievedUserData;
                return retrievedUserData;
            }
            else
            {
                return null;
            }
        }
    }
}
