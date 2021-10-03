using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.ModifiersManager;

namespace ServerCore
{
    public static class Runes_MessagingManager
    {
        #region Incoming Messages


        #endregion


        #region Outcoming Messages
        public static void NotifyAllPlayersOnRuneEffectExpiraion(Player ownerOfExpiredRune, Rune runeExpired)
        {
            string message = $"{RUNE_EFFECT_EXPIRED}|{ownerOfExpiredRune.client.userData.db_id}|{runeExpired}";
            ownerOfExpiredRune.playroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }
        public static void NotifyAllPlayersOnNewSpawnedRune(Playroom activePlayroom, Vector3 runeSpawnPos, Rune runeType, int runeId)
        {
            string message = $"{RUNE_SPAWNED}|{runeSpawnPos.X}/{runeSpawnPos.Y}/{runeSpawnPos.Z}|{runeType}|{runeId}";
            activePlayroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }
        public static void NotifyAllPlayersOnPlayerPickingUpRune(int runeId, Player runePicker, Rune runeType)
        {
            string message = $"{RUNE_PICKED_UP}|{runeId}|{runePicker.client.userData.db_id}|{runeType}|{runePicker.client.userData.nickname}|{ModifiersManager.RUNE_DURATION_SEC}";
            runePicker.playroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }

        #endregion

    }
}
