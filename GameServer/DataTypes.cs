using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ServerCore.ModifiersManager;

namespace ServerCore
{
    public static class DataTypes
    {
        public enum Rune
        {
            Random = -1,
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
        public enum CustomRuneSpawn_Amount
        {
            One,
            Three,
            Five,
            Max
        }
        public enum CustomRuneSpawn_Position
        {
            ClosestSpawn,
            Random
        }

        #region Message from server to client

        public enum MessageFromServer_WindowType
        {
            ModalWindow,
            LightWindow
        }

        public enum MessageFromServer_MessageType
        {
            Info,
            Warning,
            Error
        }

        #endregion
    }
}
