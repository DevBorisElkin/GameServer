﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public static class NetworkingMessageAttributes
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
        // user data: id, login, password, nickname, ip
        // "log_in_result|Success|1,Bond_EA,test_password,Boris228,198.0.0.196";
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
        // user data: id,login,password,nickname,ip
        public const string REGISTER_RESULT = "register_result";


        // ____________________________________________________________________________

        // code for letting know the server, that a player wants to join that playroom
        // example of message if playroom has no password
        //       code    playroom_id
        // "enter_playroom|3251";
        // example of message if playroom Has password
        //     code   playroom_id  playroom_password
        // "enter_playroom|3251|the_greatest_password_ever";
        public const string ENTER_PLAY_ROOM = "enter_playroom";

        // code for letting know the server, that a player wants to create a playroom
        // example of message
        // "create_playroom|nameOfRoom|is_public|password|map|maxPlayers";
        public const string CREATE_PLAY_ROOM = "create_playroom";

        // confirmation code for the player that he got accepted to the playroom
        // example of message
        // "confirm_enter_playroom|id/nameOfRoom/is_public/password/map/currentPlayers/maxPlayers|{fullFataOfPlayersInThatRoom}|maxJumpsAmount|initialSpawnPosition"
        // {fullFataOfPlayersInThatRoom} => ip/nickname/kills/deaths@ip/nickname/kills/deaths@ip/nickname/kills/deaths
        public const string CONFIRM_ENTER_PLAY_ROOM = "confirm_enter_playroom";

        // code for the player that playroom entering was rejected
        // example of message
        // "reject_enter_playroom|reason_of_rejection_message|";
        public const string REJECT_ENTER_PLAY_ROOM = "reject_enter_playroom";

        // message for all other players that the player joined playroom
        // example of message
        //         the code, playroom id, spawn coordinates, nickname
        // "client_connected_to_playroom|2342|0,0,0|nickname|ip"
        public const string CLIENT_CONNECTED_TO_THE_PLAYROOM = "client_connected_to_playroom";

        // message for all other players that the player disconnected from playroom
        // example of message
        //
        // when player sends to the server
        // client_disconnected_from_playroom|1|no_nickname
        // when server sends to all other clients
        //         the code, playroom id, nickname
        // "client_disconnected_from_playroom|2342|nickname|ip" 
        // TODO REMOVE UNNECESARRY ITEMS
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

        // message from client - request to get active playrooms data
        // example of message
        //
        // "playrooms_data_request"
        public const string PLAYROOMS_DATA_REQUEST = "playrooms_data_request";

        // message to the client about active playrooms
        // example of message
        //
        // "playrooms_data_response|playroom_data(/),playroom_data, playroom_data"
        // data: id/nameOfRoom/is_public/password/map/currentPlayers/maxPlayers
        public const string PLAYROOMS_DATA_RESPONSE = "playrooms_data_response";

        public static string[] MessagesFromClient_RelatedToPlayroom = new string[8]
        {
            PLAYROOMS_DATA_REQUEST,
            ENTER_PLAY_ROOM,
            CREATE_PLAY_ROOM,
            CLIENT_SHARES_PLAYROOM_POSITION,
            CLIENT_DISCONNECTED_FROM_THE_PLAYROOM,
            SHOT_REQUEST,
            JUMP_REQUEST,
            PLAYER_DIED
        };
        public static bool DoesMessageRelatedToPlayroomManager(string message)
        {
            foreach (string a in MessagesFromClient_RelatedToPlayroom)
            {
                if (message.StartsWith(a)) return true;
            }
            return false;
        }

        // Messages that client receives from server, related to playroom action
        public static string[] MessagesToClient_RelatedToPlayroom = new string[8]
        {
            MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM,
            CLIENT_DISCONNECTED_FROM_THE_PLAYROOM,
            SHOT_RESULT,
            JUMP_RESULT,
            JUMP_AMOUNT,
            PLAYERS_SCORES_IN_PLAYROOM,
            PLAYER_REVIVED,
            SPAWN_DEATH_PARTICLES
        };
        public static bool DoesMessageRelatedToOnlineGameManager(string message)
        {
            foreach (string a in MessagesToClient_RelatedToPlayroom)
            {
                if (message.StartsWith(a)) return true;
            }
            return false;
        }
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

        // _______________________ACTIVE_ACTIONS_IN_PLAYROOM_______________________

        // message to server - request to make a shot
        // we already know ID of player who makes shot
        // code|posOfShootingPoint|rotationAtRequestTime
        // "shot_request|123/45/87|543/34/12";
        public const string SHOT_REQUEST = "shot_request";

        // message to players - shows shot data
        // code|posOfShootingPoint|rotationAtRequestTime|ipOfShootingPlayer
        // "shot_result|123/45/87|543/34/1|198.0.0.126";
        public const string SHOT_RESULT = "shot_result";

        // message to server - request to make a jump
        // we already know ID of player who wants to jump
        // "jump_request -- just a code, that's all that required
        public const string JUMP_REQUEST = "jump_request";

        // message to player, result of jumping request
        // "jump_result|4 // 4 = current available amount of jumps
        public const string JUMP_RESULT = "jump_result";

        // message to player, informing on new amount of jumps
        // "jump_amount|2|true // 2 = current available amount of jumps // true = setAfterRevive
        public const string JUMP_AMOUNT = "jump_amount";

        // _______________________PLAYERS_SCORE_IN_PLAYROOM_______________________
        // message to players with scores of all existing players in the playroom
        // players_scores|data@data@data
        // {fullFataOfPlayersInThatRoom} => ip/nickname/kills/deaths@ip/nickname/kills/deaths@ip/nickname/kills/deaths
        public const string PLAYERS_SCORES_IN_PLAYROOM = "players_scores";

        // message to server, informing that the player has died
        // "player_died|killer_ip|reasonOfDeath
        public const string PLAYER_DIED = "player_died";

        // message to the player, informing that he has been revived
        // "player_revived|0/0/0|current_amount_of_jumps
        public const string PLAYER_REVIVED = "player_revived";

        // message to all players to spawn death particles
        // "sp_d_p|0/0/0|0/0/0
        public const string SPAWN_DEATH_PARTICLES = "sp_d_p";
    }
}