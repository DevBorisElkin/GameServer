using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.Runes_MessagingManager;

namespace ServerCore
{
    public class ModifiersManager
    {
        Player currentPlayer;
        public enum Rune {
            None = 0,
            Black = 1, // projectile modifier
            SpringGreen = 2, // movement modifier
            DarkGreen = 3, // movement modifier
            LightBlue = 4, // projectile modifier
            Red = 5, // projectile modifier
            Golden = 6, // attack modifier
            RedViolet = 7, // attack modifier
            Salmon = 8 // movement modifier
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
            foreach(var a in activeRuneEffects)
            {
                if (a.IsExpired())
                {
                    //rune effect has expired, notify all players and remove from list
                    NotifyAllPlayersOnRuneEffectExpiraion(currentPlayer, a.assignedRune);
                    activeRuneEffects.Remove(a);
                }
            }
        }

        public float GetReloadTimeMultiplier()
        {
            float basicMultiplier = 1f;
            if (PlayerHasEffect(Rune.Black, out RuneEffect runeEffect)) basicMultiplier += ReloadMult_BlackRune;
            if (PlayerHasEffect(Rune.Golden, out RuneEffect runeEffect2)) basicMultiplier += ReloadMult_GoldenRune;
            return basicMultiplier;
        }

        bool PlayerHasEffect(Rune rune, out RuneEffect runeEffect)
        {
            foreach (var a in activeRuneEffects)
                if (a.assignedRune == rune) { runeEffect = a; return true; }
            runeEffect = null;
            return false;
        }

        public void AddRuneEffectOnPlayer(Rune rune)
        {
            if (PlayerHasEffect(rune, out RuneEffect runeEffect))
            {
                // prolongue existing rune effect
                runeEffect.assignedTime = RUNE_DURATION_SEC;
                runeEffect.timeStarted = DateTime.Now;
            }
            else
            {
                // add comppletely new
                runeEffect.assignedRune = rune;
                runeEffect.assignedTime = RUNE_DURATION_SEC;
                runeEffect.timeStarted = DateTime.Now;
            }
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
