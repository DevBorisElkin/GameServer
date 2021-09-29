using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using static ServerCore.NetworkingMessageAttributes;

namespace GameServer
{
    public class ModifiersManager
    {
        Player currentPlayer;
        public enum Rune { 
            Black, // projectile modifier
            SpringGreen, // movement modifier
            DarkGreen, // movement modifier
            LightBlue, // projectile modifier
            Red, // projectile modifier
            Golden, // attack modifier
            RedViolet, // attack modifier
            Salmon // movement modifier
        }

        public const float RUNE_DURATION_SEC = 60f;

        public const float ReloadMult_BlackRune = 0.42f;
        public const float ReloadMult_GoldenRune = -0.5f;

        public List<RuneEffect> activeRuneEffects;

        public ModifiersManager(Player player)
        {
            currentPlayer = player;
            ResetModifiers();
        }

        // is called on entering playroom or on death
        public void ResetModifiers()
        {
            activeRuneEffects = new List<RuneEffect>();
        }


        public void CheckRuneEffectsExpiration()
        {
            List<RuneEffect> runeEffectsToRemove = new List<RuneEffect>();
            foreach(var a in activeRuneEffects)
            {
                if (a.IsExpired())
                {
                    //rune effect has expired, notify all players and remove from list
                    NotifyAllPlayersOnRuneExpiraion(a.assignedRune);
                    runeEffectsToRemove.Add(a);
                }
            }

            foreach(var a in runeEffectsToRemove)
                activeRuneEffects.Remove(a);
        }

        public float GetReloadTimeMultiplier()
        {
            float basicMultiplier = 1f;
            if (PlayerHasEffect(Rune.Black)) basicMultiplier += ReloadMult_BlackRune;
            if (PlayerHasEffect(Rune.Golden)) basicMultiplier += ReloadMult_GoldenRune;
            return basicMultiplier;
        }

        bool PlayerHasEffect(Rune rune)
        {
            foreach(var a in activeRuneEffects)
                if (a.assignedRune == rune) return true;
            return false;
        }

        void NotifyAllPlayersOnRuneExpiraion(Rune runeExpired)
        {
            string message = $"{RUNE_EFFECT_EXPIRED}|{currentPlayer.client.userData.db_id}|{runeExpired}";
            currentPlayer.playroom.SendMessageToAllPlayersInPlayroom(message, null, Util_Server.MessageProtocol.TCP);
        }


        public class RuneEffect
        {
            public Rune assignedRune;
            public DateTime timeStarted;
            public float assignedTime;

            public RuneEffect(Rune assignedRune, DateTime timeStarted, float assignedTime)
            {
                this.assignedRune = assignedRune;
                this.timeStarted = timeStarted;
                this.assignedTime = assignedTime;
            }

            public bool IsExpired()
            {
                var msSinceRuneCountdownStarted = (DateTime.Now - timeStarted).TotalMilliseconds;
                if (msSinceRuneCountdownStarted >= TimeSpan.FromSeconds(assignedTime).TotalMilliseconds) return true;
                return false;
            }

            public float TimeLeftForTheEffect()
            {
                var secSinceRuneCountdownStarted = (DateTime.Now - timeStarted).TotalSeconds;
                float remainingTime = assignedTime - (float)secSinceRuneCountdownStarted;
                if (remainingTime < 0) remainingTime = 0;
                return remainingTime;
            }
        }
    }
}
