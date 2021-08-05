using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer
{
    class NetworkingMessageAttributes
    {
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
