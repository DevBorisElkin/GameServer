﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer
{
    class NetworkingMessageAttributes
    {

        //__AUTHENTICATION________________________________________________________

        // message to server from player that he wants to log into his account
        // example of message
        // "log_in|user_login|user_password";
        public const string LOG_IN = "log_in";

        // message from server to player whether player's request was accepted or not
        // example of message IF SUCCESS
        // "log_in_result|success_or_failure_plus_reason_if_failure|complete_user_data";

        // example of message IF FAIL
        // "log_in_result|success_or_failure_plus_reason_if_failure";

        // more detailed response
        // user data: id, login, password, nickname
        // "log_in_result|Success|1,Bond_EA,test_password,Boris228";
        // "log_in_result|Fail_WrongPairLoginPassword"
        public const string LOG_IN_RESULT = "log_in_result";

        //__REGISTRATION__________________________________________________________

        // message to server from player that he wants to register new account
        // example of message
        // "register_request|user_login|user_password|user_nickname";
        public const string REGISTER = "register_request";

        // message from server to player whether player's request was accepted or not
        // example of message
        // "register_result|success_or_failure_plus_reason_if_failure|complete_user_data";
        // user data: id,login,password,nickname
        public const string REGISTER_RESULT = "register_result";


        // ____________________________________________________________________________

        // code for letting know the server, that a player wants to join that playroom
        // example of message
        // "enter_playroom|1|nickname";
        public const string ENTER_PLAY_ROOM = "enter_playroom";

        // confirmation code for the player that he got accepted to the playroom
        // example of message
        // "confirm_enter_playroom|1|";
        public const string CONFIRM_ENTER_PLAY_ROOM = "confirm_enter_playroom";

        // message for all other players that the player joined playroom
        // example of message
        //         the code, playroom number, spawn coordinates, nickname
        // "client_connected_to_playroom|1|0,0,0|nickname|ip"
        public const string CLIENT_CONNECTED_TO_THE_PLAYROOM = "client_connected_to_playroom";

        // message for all other players that the player disconnected from playroom
        // example of message
        //         the code, playroom number, nickname
        // "client_connected_to_playroom|1|nickname|ip"
        public const string CLIENT_DISCONNECTED_FROM_THE_PLAYROOM = "client_disconnected_from_playroom";

        // message from client to server about client position and rotation
        // example of message
        //         the code, playroom number, coordinates, rotation
        // "client_shares_playroom_position|0/0/0|0/0/0"
        public const string CLIENT_SHARES_PLAYROOM_POSITION = "client_shares_playroom_position";

        // message to all clients about other clients in playroom position and rotation
        // example of message
        //
        // "players_positions_in_playroom|nickname,ip,position,rotation@nickname,ip,position,rotation@enc..."
        public const string MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM = "players_positions_in_playroom";

        // _______________________________________________________CONNECTION_CHECK________________________


        // code for checking if player is connected
        // example of message receiving on server to confirm
        // "check_connected";
        public const string CHECK_CONNECTED = "check_connected";

        // code for checking if player is in playroom
        // example of message receiving on server to confirm
        // "check_connected_playroom|1";
        public const string CHECK_CONNECTED_PLAYROOM = "check_connected_playroom";

        // message for client that he was disconnected
        // example of message receiving on server to confirm
        // "client_disconnected
        public const string CLIENT_DISCONNECTED = "client_disconnected";

        // string that is attached to the end of each message sent
        // "client_disconnected<EOF>
        public const string END_OF_FILE = "<EOF>";
    }
}
