using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.Runes_MessagingManager;
using static ServerCore.DataTypes;

namespace ServerCore
{
    public class ModifiersManager
    {
        Player currentPlayer;
        
        public const float RUNE_DURATION_SEC = 60f;

        public const float ReloadMult_BlackRune = 0.42f;
        public const float ReloadMult_GoldenRune = -0.5f;

        public List<RuneEffect> activeRuneEffects;

        public ModifiersManager(Player player)
        {
            currentPlayer = player;
            ResetModifiers();
        }

        #region Modifiers basics
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
                    NotifyAllPlayersOnRuneEffectExpiraion(currentPlayer, a.assignedRune);
                    runeEffectsToRemove.Add(a);
                }
            }

            foreach(var a in runeEffectsToRemove)
                activeRuneEffects.Remove(a);
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
                runeEffect = new RuneEffect(rune, DateTime.Now, RUNE_DURATION_SEC);
                activeRuneEffects.Add(runeEffect);
            }
        }
        #endregion

        #region Interactive part

        public float GetReloadTimeMultiplier()
        {
            float basicMultiplier = 1f;
            if (PlayerHasEffect(Rune.Black, out RuneEffect runeEffect)) basicMultiplier += ReloadMult_BlackRune;
            if (PlayerHasEffect(Rune.Golden, out RuneEffect runeEffect2)) basicMultiplier += ReloadMult_GoldenRune;
            return basicMultiplier;
        }

        public string ShotModifiersIntoString()
        {
            string onShotModifiers = "";

            int counter = 0;
            for (int i = 0; i < activeRuneEffects.Count; i++)
            {
                if (activeRuneEffects[i].assignedRune.Equals(Rune.Black) || activeRuneEffects[i].assignedRune.Equals(Rune.LightBlue) ||
                    activeRuneEffects[i].assignedRune.Equals(Rune.Red))
                {
                    if (counter > 0) onShotModifiers += "/";
                    onShotModifiers += activeRuneEffects[i].assignedRune.ToString();
                    counter++;
                }
            }
            if (string.IsNullOrEmpty(onShotModifiers)) onShotModifiers = "none";
            return onShotModifiers;
        }


        #endregion
    }
}
