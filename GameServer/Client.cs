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
        public UserData userData;
        public Player player;
        public Client(ClientHandler _ch)
        {
            ch = _ch;
            clientAccessLevel = ClientAccessLevel.LowestLevel;
        }

        public void DestroyClient()
        {
            ch = null;
            userData = null;
            player = null;
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

        public async Task<UserData> RefreshUserData()
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
    }
}
