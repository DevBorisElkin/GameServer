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
        public static void NotifyAllPlayersOnRuneEffectExpiraion(Player ownerOfExpiredRune, DataTypes.Rune runeExpired)
        {
            string message = $"{RUNE_EFFECT_EXPIRED}|{ownerOfExpiredRune.client.userData.db_id}|{runeExpired}";
            ownerOfExpiredRune.playroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }
        public static void NotifyAllPlayersOnNewSpawnedRune(Playroom activePlayroom, Vector3 runeSpawnPos, DataTypes.Rune runeType, int runeId, string dbIdOfInvoker = "none")
        {
            string message = $"{RUNE_SPAWNED}|{runeSpawnPos.X}/{runeSpawnPos.Y}/{runeSpawnPos.Z}|{runeType}|{runeId}|{dbIdOfInvoker}";
            activePlayroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }
        public static void NotifyAllPlayersOnPlayerPickingUpRune(int runeId, Player runePicker, DataTypes.Rune runeType)
        {
            string message = $"{RUNE_PICKED_UP}|{runeId}|{runePicker.client.userData.db_id}|{runeType}|{runePicker.client.userData.nickname}|{ModifiersManager.RUNE_DURATION_SEC}";
            runePicker.playroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }

        public static void NotifyNewlyConnectedPlayerOfExistingRunes(Player player, Playroom activePlayroom)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(RUNES_INFO + "|");
            int i = 0;
            int runesCount = 0;
            foreach(var a in activePlayroom.runesManager.runeSpawns)
            {
                if(a.currentRune != null)
                {
                    runesCount++;
                    if (i != 0) sb.Append("@");
                    sb.Append($"{a.PositionToString()},{a.currentRune.rune},{a.currentRune.runeUniqueId}");
                    i++;
                }
            }
            if(runesCount > 0)
                Util_Server.SendMessageToClient(sb.ToString(),player.client.ch, Util_Server.MessageProtocol.TCP);
        }

        public static void NotifyNewlyConnectedPlayerOfPlayersRuneEffects(Player player, Playroom activePlayroom)
        {
            string message = $"{RUNE_EFFECTS_INFO}|";
            string addon = activePlayroom.CurrentRuneEffectsToString(player);
            if(addon != "none")
            {
                message += addon;
                Util_Server.SendMessageToClient(message, player.client.ch, Util_Server.MessageProtocol.TCP);
            }
        }

        #endregion

    }
}
