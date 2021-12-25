using System;
using static ServerCore.Util_Server;
using System.Numerics;
using System.Globalization;
using ServerCore;
using DatabaseAccess;
using System.Collections.Generic;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.Server;
using static ServerCore.PlayroomManager_MapData;
using static ServerCore.Client;
using static EntryPoint.ConsoleInput;
using static ServerCore.DataTypes;
using System.Threading.Tasks;

namespace EntryPoint
{
    class Program
    {
        public static ServerSimpleImplementation server;
        static void Main(string[] args)
        {
            server = new ServerSimpleImplementation();
        }
        // SIMPLE AND QUICK IMPLEMENTATION
        public class ServerSimpleImplementation
        {
            int portTcp = 8384;
            int portUdp = 8385;
            public ServerSimpleImplementation()
            {
                DatabaseBridge.InitDatabase();
                PlayroomManager.InitPlayroomManager();
                SubscribeToEvents();
                connected_clients = new List<Client>();
                Server.StartServer(portTcp, portUdp);
                while (true) ReadConsole();
            }
            void SubscribeToEvents()
            {
                OnServerStartedEvent += ServerStarted;
                OnServerShutDownEvent += ServerShutDown;
                OnClientConnectedEvent += ClientConnected;
                OnClientDisconnectedEvent += ClientDisconnected;
                OnMessageReceivedEvent += Connection_MessageReceived;
            }



            void ServerStarted() { Console.WriteLine($"[{DateTime.Now}][SERVER_LAUNCHED][{Server.ip}]"); }
            void ServerShutDown() { Console.WriteLine($"[{DateTime.Now}][SERVER_SHUTDOWN][{Server.ip}]"); }
            void ClientConnected(ClientHandler clientHandler)
            {
                Console.WriteLine($"[{DateTime.Now}][CLIENT_CONNECTED][{clientHandler.connectionID}][{clientHandler.ip}]");
                connected_clients.Add(new Client(clientHandler));

            }
            void ClientDisconnected(ClientHandler clientHandler, string error)
            {
                Console.WriteLine($"[{DateTime.Now}][CLIENT_DISCONNECTED][{clientHandler.connectionID}][{clientHandler.ip}]: {error}");
                RemoveClient(clientHandler);
            }

            #region Parce Messages From Clients

            // false if account is already taken or error accessing database
            async static Task<RequestResult> CheckClientAlreadyAuthenticated(Client initial, string login)
            {
                UserData data = await DatabaseBridge.GetUserData(login);
                if (data.requestResult == RequestResult.Success)
                {
                    foreach (var a in connected_clients)
                        if (a != initial && a.userData != null && a.userData.db_id == data.db_id) return RequestResult.Fail_LoginAlreadyTaken;
                    return RequestResult.Success;
                }
                return data.requestResult;
            }

            public async static void Connection_MessageReceived(string msg, ClientHandler ch, MessageProtocol mp)
            {
                Client assignedClient = GetClientByClientHandler(ch);
                if (assignedClient == null) { Console.WriteLine($"[{DateTime.Now}]Error, ch is not assigned to client"); return; }

                int clientDbId = -1;
                if (assignedClient.userData != null) clientDbId = assignedClient.userData.db_id;

                string[] parcedMessage = msg.Split(END_OF_FILE, StringSplitOptions.RemoveEmptyEntries);

                foreach (string message in parcedMessage)
                {
                    try
                    {
                        if (message.Contains(CHECK_CONNECTED)) continue;

                        // not showing CHECK_CONNECTED and SHARES_PLAYROOM because it spams in console
                        if (!message.Contains(CLIENT_SHARES_PLAYROOM_POSITION) && !message.Contains(SHOT_REQUEST) && !message.Contains(JUMP_REQUEST))
                        {

                            Console.WriteLine($"[{DateTime.Now}][CLIENT_MESSAGE][{mp}][{clientDbId}][{ch.ip}]: {message} | {DateTime.Now}");
                        }

                        // _*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*

                        if (assignedClient.clientAccessLevel.Equals(ClientAccessLevel.LowestLevel))
                        {
                            // accept only requests for authentication and registration
                            if (message.Contains(LOG_IN))
                            {
                                string[] substrings = message.Split("|");

                                // 1) Check if user's account is already in use

                                RequestResult rr = await CheckClientAlreadyAuthenticated(assignedClient, substrings[1]);

                                if (rr.Equals(RequestResult.Success) || rr.Equals(RequestResult.Fail_NoUserWithGivenLogin))
                                {
                                    await DatabaseBridge.TryToAuthenticateAsync(substrings[1], substrings[2], assignedClient);
                                    return;
                                }
                                else if (rr.Equals(RequestResult.Fail_LoginAlreadyTaken))
                                {
                                    Misc_MessagingManager.SendMessageToTheClient("You can't access this account because user with specified account is currently connected", assignedClient.ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Warning);
                                    return;
                                }
                                else if (rr.Equals(RequestResult.Fail_NoConnectionToDB))
                                {
                                    Misc_MessagingManager.SendMessageToTheClient("Server lost connection to the Database and can't perform this operation, please try again later..", assignedClient.ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Warning);
                                    return;
                                }
                                else
                                {
                                    Misc_MessagingManager.SendMessageToTheClient("Unknown error, please try again later..", assignedClient.ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Warning);
                                    return;
                                }
                            }
                            else if (message.Contains(REGISTER))
                            {
                                string[] substrings = message.Split("|");
                                DatabaseBridge.TryToRegisterAsync(substrings[1], substrings[2], substrings[3], assignedClient);
                            }
                            else
                            {
                                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: 1) It appears that client [{clientDbId}][{ch.ip}] " +
                                    $"asks operation that he has no rights for, his request: {message}");
                            }

                        }
                        else if (assignedClient.clientAccessLevel.Equals(ClientAccessLevel.Authenticated))
                        {
                            // accept all other requests
                            if (DoesMessageRelatedToPlayroomManager(message))
                            {
                                ParceMessage_Playroom(message, assignedClient);
                            }
                            else if (message.StartsWith(PROMOCODE_FROM_CLIENT))
                            {
                                string[] substrings = message.Split("|");
                                if (substrings[1].Equals(SUBCODE_GET_ADMIN_RIGHTS))
                                {
                                    UserData data = await assignedClient.RefreshUserDataFromDatabase();
                                    if(data != null)
                                    {
                                        if(data.accessRights == AccessRights.Admin || data.accessRights == AccessRights.SuperAdmin)
                                            Misc_MessagingManager.SendMessageToTheClient($"Can't upgrade your user rights because you already have level {data.accessRights}", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Info);
                                        else
                                        {
                                            UserData updatedUserData = await assignedClient.UpdateUserData_AccessRights(AccessRights.Admin);
                                            if(updatedUserData != null && updatedUserData.accessRights == AccessRights.Admin)
                                            {
                                                Misc_MessagingManager.SendMessageToTheClient($"Congratulations, your rights have been upgraded!", ch, MessageFromServer_WindowType.ModalWindow, MessageFromServer_MessageType.Info);
                                                string msg_access_rights = $"{NEW_ACCESS_RIGHTS_STATUS}|{updatedUserData.accessRights}";
                                                Util_Server.SendMessageToClient(msg_access_rights, assignedClient.ch);
                                            }
                                            else
                                            Misc_MessagingManager.SendMessageToTheClient($"Couldn't update data in database, try again later", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                        }
                                    }
                                    else
                                    {
                                        Misc_MessagingManager.SendMessageToTheClient("Unfortunately server couldn't retrieve user data, try again later", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                    }
                                } else if (substrings[1].Equals(SUBCODE_DOWNGRADE_TO_USER_RIGHTS))
                                {
                                    UserData data = await assignedClient.RefreshUserDataFromDatabase();
                                    if (data != null)
                                    {
                                        if (data.accessRights == AccessRights.User)
                                            Misc_MessagingManager.SendMessageToTheClient($"Can't downgrade your user rights because you already have level {data.accessRights}", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Info);
                                        else
                                        {
                                            UserData updatedUserData = await assignedClient.UpdateUserData_AccessRights(AccessRights.User);
                                            if (updatedUserData != null && updatedUserData.accessRights == AccessRights.User)
                                            {
                                                Misc_MessagingManager.SendMessageToTheClient($"Your rights have been successfully downgraded", ch, MessageFromServer_WindowType.ModalWindow, MessageFromServer_MessageType.Info);
                                                string msg_access_rights = $"{NEW_ACCESS_RIGHTS_STATUS}|{updatedUserData.accessRights}";
                                                Util_Server.SendMessageToClient(msg_access_rights, assignedClient.ch);
                                            }
                                            else Misc_MessagingManager.SendMessageToTheClient($"Couldn't update data in database, try again later", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                        }
                                    }
                                    else Misc_MessagingManager.SendMessageToTheClient("Unfortunately server couldn't retrieve user data, try again later", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                }
                                else if (substrings[1].Equals(SUBCODE_CHANGE_NICKNAME))
                                {
                                    // check new proposed nickname.. substrings[2]
                                    InputCompatibilityCheck result = Util_Server.CheckInputField(substrings[2], out string errorString);
                                    if(result != InputCompatibilityCheck.Success)
                                    {
                                        Misc_MessagingManager.SendMessageToTheClient($"Can't change nickname, {errorString}", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Warning);
                                        return;
                                    }

                                    UserData data = await assignedClient.RefreshUserDataFromDatabase();
                                    if (data != null)
                                    {
                                        UserData updated = await assignedClient.UpdateUserData_Nickname(substrings[2]);
                                        if(updated.requestResult == RequestResult.Success)
                                        {
                                            Misc_MessagingManager.SendMessageToTheClient($"Successfully changed nickname to {substrings[2]}", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Info);
                                            // TODO
                                        }
                                        else if(updated.requestResult == RequestResult.Fail_NicknameAlreadyTaken)
                                            Misc_MessagingManager.SendMessageToTheClient($"Can't change nickname because it's already taken", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Warning);
                                        else if (updated.requestResult == RequestResult.Fail_NoConnectionToDB)
                                            Misc_MessagingManager.SendMessageToTheClient($"Can't change nickname, no connection to Database", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                        else
                                            Misc_MessagingManager.SendMessageToTheClient($"Can't change nickname, unknown reason", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                    }
                                    else Misc_MessagingManager.SendMessageToTheClient("Unfortunately server couldn't retrieve user data, try again later", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                }
                                else Misc_MessagingManager.SendMessageToTheClient("Unknown promocode, try something else :)", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                            }
                            else if (message.StartsWith(GET_USER_DATA_REQUEST))
                            {
                                string[] substrings = message.Split("|");
                                int db_id = Int32.Parse(substrings[1]);
                                UserData data = await Client.GetUserDataFromDatabase(db_id);
                                if(data == null)
                                {
                                    Misc_MessagingManager.SendMessageToTheClient($"Couldn't get user data from database, try again later", ch, MessageFromServer_WindowType.LightWindow, MessageFromServer_MessageType.Error);
                                    return;
                                }

                                string dataString = assignedClient.userData.db_id == db_id ? data.ToNetworkString() : data.ToNetworkStringSecured();
                                Util_Server.SendMessageToClient($"{GET_USER_DATA_RESULT}|{dataString}", assignedClient.ch);
                            }
                            else
                            {
                                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: 2) It appears that client [{clientDbId}][{ch.ip}] " +
                                    $"asks operation that he has no rights for, his request: {message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}]{e.Message} ||| {e.StackTrace} ||| message was:{message}");
                    }
                }
            }

            public static void ParceMessage_Playroom(string message, Client client)
            {
                try
                {
                    if (message.StartsWith(PLAYROOMS_DATA_REQUEST))
                    {
                        PlayroomManager.RequestFromClient_GetPlayroomsData(client);
                    }
                    else if (message.StartsWith(CREATE_PLAY_ROOM))
                    {
                        string[] substrings = message.Split("|");

                        bool.TryParse(substrings[2], out bool isPublic);
                        Enum.TryParse(substrings[4], out Map map);

                        PlayroomManager.RequestFromClient_CreatePlayroom(client, substrings[1], isPublic, substrings[3], map,
                         Int32.Parse(substrings[5]), Int32.Parse(substrings[6]), Int32.Parse(substrings[7]), Int32.Parse(substrings[8]));
                    }
                    else if (message.StartsWith(ENTER_PLAY_ROOM))
                    // normally here should be some logic, checking, if specific playroom has space for new players to join
                    {
                        string[] substrings = message.Split("|");

                        Console.WriteLine($"[{DateTime.Now}][{client.ch.connectionID}][{client.ch.ip}] Client requested to connect to playroom");
                        if (substrings.Length == 2)
                            PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), client);
                        else if (substrings.Length == 3)
                            PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), client, substrings[2]);
                    }
                    else if (message.StartsWith(CLIENT_SHARES_PLAYROOM_POSITION))
                    {
                        string[] substrings = message.Split("|");
                        string[] positions = substrings[1].Split("/");
                        Vector3 position = new Vector3(
                            float.Parse(positions[0], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(positions[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(positions[2], CultureInfo.InvariantCulture.NumberFormat));

                        string[] rotations = substrings[2].Split("/");
                        Quaternion rotation = new Quaternion(
                            float.Parse(rotations[0], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(rotations[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(rotations[2], CultureInfo.InvariantCulture.NumberFormat),
                            0);

                        PlayroomManager.RequestFromClient_StorePlayerPositionAndRotation(client, position, rotation);
                    }
                    else if (message.StartsWith(CLIENT_DISCONNECTED_FROM_THE_PLAYROOM))
                    {
                        Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]:Client [{client.ch.connectionID}][{client.ch.ip}] disconnected from playroom");
                        string[] substrings = message.Split("|");
                        PlayroomManager.RequestFromClient_DisconnectFromPlayroom(int.Parse(substrings[1]), client);
                    }
                    else if (message.StartsWith(SHOT_REQUEST))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.CheckAndMakeShot(message);
                        }
                    }
                    else if (message.StartsWith(JUMP_REQUEST))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.CheckAndMakeJump();
                        }
                    }
                    else if (message.StartsWith(PLAYER_DIED))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.PlayerDied(message);
                        }
                    }
                    else if (message.StartsWith(RUNE_TRY_TO_PICK_UP))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.PlayerTriesToPickUpRune(message);
                        }
                    }
                    else if (message.StartsWith(PLAYER_RECEIVED_DEBUFF))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.PlayerReceivedDebuff(message);
                        }
                    }
                    else if (message.StartsWith(PLAYER_DEBUFF_ENDED))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.PlayeDebuffEnded(message);
                        }
                    }else if (message.StartsWith(ADMIN_COMMAND_SPAWN_RUNE))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            if(client.userData.accessRights.Equals(AccessRights.Admin) || client.userData.accessRights.Equals(AccessRights.SuperAdmin))
                            {
                                client.player.playroom.AdminCommand_SpawnRunes(message, client.player);
                            }
                            else Console.WriteLine($"[{DateTime.Now}][AdminCommands]: Will NOT execute command because user has no rights for it. Message[{message}]");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            #endregion
        }
    }
}
