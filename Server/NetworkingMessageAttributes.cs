using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public static class NetworkingMessageAttributes
    {
        // On connection established, sending client his local id in clients list
        // "on_connection_established|12"  // 12 = local id in clients list
        public const string ON_CONNECTION_ESTABLISHED = "on_connection_established";

        // Initializing UDP
        // "init_udp|12"  // 12 = local id in clients list
        public const string INIT_UDP = "init_udp";

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
        // user data: db_id, login, password, nickname, ip, accessRights
        // "log_in_result|Success|1,Bond_EA,test_password,Boris,228,198.0.0.196,user";
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
        // user data: db_id,login,password,nickname,ip,accessRights
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
        // "create_playroom|nameOfRoom|is_public|password|map|maxPlayers|minPlayersToStart|killsToFinish|timeOfMatch";
        public const string CREATE_PLAY_ROOM = "create_playroom";

        // confirmation code for the player that he got accepted to the playroom
        // example of message
        // "confirm_enter_playroom|idOfRoom/nameOfRoom/is_public/password/map/currentPlayers/maxPlayers/MatchState/PlayersToStartTheMatch/TimeTillTheEndOfMatch/KillsForVictory|{fullFataOfPlayersInThatRoom}|maxJumpsAmount|initialSpawnPosition|"
        // {fullFataOfPlayersInThatRoom} => ip/nickname/kills/deaths@ip/nickname/kills/deaths@ip/nickname/kills/deaths
        public const string CONFIRM_ENTER_PLAY_ROOM = "confirm_enter_playroom";

        // code for the player that playroom entering was rejected
        // example of message
        // "reject_enter_playroom|reason_of_rejection_message|";
        public const string REJECT_ENTER_PLAY_ROOM = "reject_enter_playroom";

        // message for all other players that the player joined playroom
        // example of message
        //         the code, ip, nickname
        // "client_connected_to_playroom|192.148.65.11|Bond_EA"
        public const string CLIENT_CONNECTED_TO_THE_PLAYROOM = "client_connected_to_playroom";

        // message for all other players that the player disconnected from playroom
        // example of message
        //
        // when player sends to the server
        // client_disconnected_from_playroom|playroomId|playerNickname
        // when server sends to all other clients
        //         the code, playroom id, clientDbID
        // "client_disconnected_from_playroom|playroomId|nickname|clientDbId" 
        public const string CLIENT_DISCONNECTED_FROM_THE_PLAYROOM = "client_disconnected_from_playroom";

        // message from client to server about client position and rotation
        // it's the only incoming data sent by UDP so local id is to identify client in case UDP point changes
        // example of message
        //         the code, coordinates, rotation, local_id
        // "client_shares_playroom_position|0/0/0|0/0/0|12"
        public const string CLIENT_SHARES_PLAYROOM_POSITION = "client_shares_playroom_position";

        // message to all clients about other clients in playroom position and rotation
        // example of message
        //
        // "players_positions_in_playroom|nickname,db_id,position,rotation@nickname,db_id,position,rotation@enc..."
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

        public static string[] MessagesFromClient_RelatedToPlayroom = new string[12]
        {
            PLAYROOMS_DATA_REQUEST,
            ENTER_PLAY_ROOM,
            CREATE_PLAY_ROOM,
            CLIENT_SHARES_PLAYROOM_POSITION,
            CLIENT_DISCONNECTED_FROM_THE_PLAYROOM,
            SHOT_REQUEST,
            JUMP_REQUEST,
            PLAYER_DIED,
            RUNE_TRY_TO_PICK_UP,
            PLAYER_RECEIVED_DEBUFF,
            PLAYER_DEBUFF_ENDED,
            ADMIN_COMMAND_SPAWN_RUNE
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
        public static string[] MessagesToClient_RelatedToPlayroom = new string[21]
        {
            MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM,
            CLIENT_DISCONNECTED_FROM_THE_PLAYROOM,
            SHOT_RESULT,
            JUMP_RESULT,
            JUMP_AMOUNT,
            PLAYERS_SCORES_IN_PLAYROOM,
            PLAYER_REVIVED,
            SPAWN_DEATH_PARTICLES,
            PLAYER_WAS_KILLED_MESSAGE,
            CLIENT_CONNECTED_TO_THE_PLAYROOM,
            MATCH_STARTED,
            MATCH_STARTED_FORCE_OVERRIDE_POSITION_AND_JUMPS,
            MATCH_TIME_REMAINING,
            MATCH_FINISHED,
            RUNE_SPAWNED,
            RUNE_PICKED_UP,
            RUNE_EFFECT_EXPIRED,
            RUNES_INFO,
            RUNE_EFFECTS_INFO,
            PLAYER_RECEIVED_DEBUFF,
            PLAYER_DEBUFF_ENDED
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

        // 1) simple check connected message
        // 2) echoed check connected message with id to check ping delay
        // "check_connected
        // "check_connected_echo|2";
        public const string CHECK_CONNECTED = "check_connected";
        public const string CHECK_CONNECTED_ECHO_TCP = "check_connected_echo_tcp";
        public const string CHECK_CONNECTED_ECHO_UDP = "check_connected_echo_udp";

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
        // code|posOfShootingPoint|rotationAtRequestTime|dbIdOfShootingPlayer|activeRuneModifiers
        // activeRuneModifiers: rune@rune@rune  or "none"
        // "shot_result|123/45/87|543/34/1|13|RedViolet/Black/LightBlue/Red";
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
        // {fullFataOfPlayersInThatRoom} => db_id/nickname/kills/deaths@db_id/nickname/kills/deaths@db_id/nickname/kills/deaths
        public const string PLAYERS_SCORES_IN_PLAYROOM = "players_scores";

        // message to players notifying that a player in the playroom was killed
        // player_was_killed_message|playerDeadNickname/playerDeadDbID|playerKillerNickname/playerKillerDbId|deathDetails
        public const string PLAYER_WAS_KILLED_MESSAGE = "player_was_killed_message";

        // message to server, informing that the player has died
        // "player_died|killer_dbId|deathDetails
        public const string PLAYER_DIED = "player_died";

        // message to the player, informing that he has been revived
        // "player_revived|0/0/0|current_amount_of_jumps
        public const string PLAYER_REVIVED = "player_revived";

        // message to all players to spawn death particles
        // "sp_d_p|0/0/0|0/0/0
        public const string SPAWN_DEATH_PARTICLES = "sp_d_p";

        // additional message to all other players to help to create debuff particles
        // code|playerWhoGotDebuffsDbId|LightBlue
        // "player_received_debuff|12|LightBlue
        public const string PLAYER_RECEIVED_DEBUFF = "player_received_debuff";

        // additional message to all other players to help to cancel debuff particles
        // code|playerWhoGotDebuffsDbId|LightBlue
        // "player_debuff_ended|12|LightBlue
        public const string PLAYER_DEBUFF_ENDED = "player_debuff_ended";


        // _______________________MATCH_STATE_AND_EVENTS_______________________
        // message to all players notifying that the match has started
        // after that message each client should receive special message, overriding his position,
        // and also each player's jumps should be resetted
        // "match_started|645      // 645 = timeTillEndOfMatchInSeconds
        public const string MATCH_STARTED = "match_started";

        // match_started_force_override|Vector3-position(/)|newJumpsAmount
        public const string MATCH_STARTED_FORCE_OVERRIDE_POSITION_AND_JUMPS = "match_started_force_override";

        // message to all players notifying how much seconds left till the end of match
        // "match_time_remaining|327 // 327 = time in seconds left
        public const string MATCH_TIME_REMAINING = "match_time_remaining";

        // message to all players notifying that the match has finished
        // "match_finished|winnerDbId|winnerNickname|matchResult
        public const string MATCH_FINISHED = "match_finished";

        // _______________________MATCH_STATE_AND_EVENTS_______________________
        // Messages related to the clients relating runes

        // ________________ Messages FROM server _____________________

        //       code   spawnPos runeType  uniqueRudeId
        // "rune_spawned|0/0/0|Black|12"
        public const string RUNE_SPAWNED = "rune_spawned";

        //  code|runeUniqueId|player_db_id|runeType|nickOfGatherPlayer|60
        // "rune_picked_up|6|12|Black|BOBISCHE|durationInSeconds"
        public const string RUNE_PICKED_UP = "rune_picked_up";

        //  code|player_db_id|runeType
        // "rune_effect_expired|12|Black"
        public const string RUNE_EFFECT_EXPIRED = "rune_effect_expired";

        //  code|rune_data@rune_data
        // "runes_info|spawnPos,runeType,uniqueRuneId@spawnPos,runeType,uniqueRuneId
        public const string RUNES_INFO = "runes_info";

        //  code|rune_effect_data@rune_effect_data
        // "rune_effects_info|player_db_id,runeType, runeType,runeType@player_db_id,runeType
        public const string RUNE_EFFECTS_INFO = "rune_effects_info";

        // ________________ Messages TO server _____________________

        //  code|player_db_id|runeType|runeUniqueId
        // "rune_try_to_pick_up|12|Black|5"
        public const string RUNE_TRY_TO_PICK_UP = "rune_try_to_pick_up";


        #region CommandsFromAdminPlayer
        // commands from admin_player to server

        // code|RuneType|AmountEnum|SpawnPosEnum|notifyOthers
        public const string ADMIN_COMMAND_SPAWN_RUNE = "admin_command_spawn_rune";


        #endregion


    }
}
