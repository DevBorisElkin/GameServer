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

        // 1) basic move speed
        // 2) basic rotation speed
        // 3) basic jump force
        // 4) basic jump force mode
        // 5) 
        public class ServerToPlayerSettings
        {
            public float basicMoveSpeed;
            public float basicRotationSpeed;
            public float basicJumpForce;
            public string basicJumpForceMode;

            public float onProjectileHit_basicPushForce;

            public float onLightBlueProjectileHit_movementMultiplier;
            public float onLightBlueProjectileHit_rotationMultiplier;
            public float onLightBlueProjectileHit_pushingPowerMultiplier;

            public float onRedProjectileHit_visionMultiplier;

            public float onLightGreenRune_MoveSpeedMultiplier;
            public float onDarkGreenRune_JumpForceMultiplier;
        }
    }
}
