using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using ServerCore;
using static ServerCore.ModifiersManager;
using static ServerCore.PlayroomManager_MapData;
using static ServerCore.Runes_MessagingManager;
using static ServerCore.DataTypes;

namespace ServerCore
{
    public class PlayroomManager_Runes
    {
        Playroom assignedPlayroom;
        List<int> usedRuneIds;
        public List<RuneSpawn> runeSpawns;
        Random random;

        public DateTime nextRuneSpawnInception;
        public float nextRuneSpawnTime;

        #region Init
        public void Init(Playroom playroom)
        {
            assignedPlayroom = playroom;
            usedRuneIds = new List<int>();
            random = new Random();
            CreateRuneSpawns(assignedPlayroom.map);
            SetNextRuneSpawnTime();

        }
        public void CreateRuneSpawns(Map map)
        {
            runeSpawns = new List<RuneSpawn>();
            MapData mapData = PlayroomManager_MapData.GetMapDataByMap(map);

            foreach (var a in mapData.runeSpawns)
                runeSpawns.Add(new RuneSpawn(a, null));
        }
        #endregion

        #region Spawn Rune

        bool hadNoFreeSpawns;
        public void Update()
        {
            bool hasFreeRuneSpawn = HasFreeRuneSpawn();
            if(hasFreeRuneSpawn && hadNoFreeSpawns)
            {
                hadNoFreeSpawns = false;
                SetNextRuneSpawnTime();
                return;
            }else if (!hasFreeRuneSpawn)
            {
                hadNoFreeSpawns = true;
                return;
            }else if (hasFreeRuneSpawn && HasTimeReachedToSpawnNextRune(out float totalTimeSpentForRuneSpawn, out float totalNextRuneSpawnPregeneratedTime))
            {
                SetNextRuneSpawnTime();
                SpawnRune(totalTimeSpentForRuneSpawn, totalNextRuneSpawnPregeneratedTime);
            }
        }

        public void SpawnRune(float timeSpentForSpawn, float totalPregeneratedTime)
        {
            int newRuneId = GetUniqueNewRuneId();
            if (newRuneId == -1) return;

            RuneSpawn chosenRuneSpawn = GetRandomRuneSpawn();
            chosenRuneSpawn.currentRune = new RuneInstance(GetRandomRuneType(), newRuneId);
            NotifyAllPlayersOnNewSpawnedRune(assignedPlayroom, chosenRuneSpawn.position, chosenRuneSpawn.currentRune.rune, newRuneId);
            Console.WriteLine($"[{DateTime.Now}][Rune spawned]:[{chosenRuneSpawn.currentRune.rune}] totalSpawnTime: [{timeSpentForSpawn}], pregeneratedSpawnTime: [{totalPregeneratedTime}]");
        }

        public int GetUniqueNewRuneId()
        {
            int runeId = 0;
            int iteration = 0;
            while (iteration < 1000)
            {
                runeId = random.Next(1, 10001);
                if (!usedRuneIds.Contains(runeId))
                {
                    usedRuneIds.Add(runeId);
                    return runeId;
                }
                iteration++;
            }
            Console.WriteLine($"[PLAYROOM_ERROR]: couldn't get unique rune id for playroom {assignedPlayroom.playroomID}");
            return -1;
        }

        #endregion

        #region Util and check rune
        public void SetNextRuneSpawnTime()
        {
            nextRuneSpawnInception = DateTime.Now;
            nextRuneSpawnTime = random.Next((int)PlayroomManager.randomRuneSpawnTime.X, (int)PlayroomManager.randomRuneSpawnTime.Y);
        }

        public bool HasTimeReachedToSpawnNextRune(out float timeSpentForRuneSpawn, out float totalRuneSpawnPregeneratedTime)
        {
            timeSpentForRuneSpawn = 0f;

            var msSinceNextSpawnInceptionTime = (DateTime.Now - nextRuneSpawnInception).TotalMilliseconds;
            if (msSinceNextSpawnInceptionTime >= TimeSpan.FromSeconds(nextRuneSpawnTime).TotalMilliseconds)
            {
                timeSpentForRuneSpawn = (float) TimeSpan.FromMilliseconds(msSinceNextSpawnInceptionTime).TotalSeconds;
                totalRuneSpawnPregeneratedTime = nextRuneSpawnTime;
                return true;
            }
            totalRuneSpawnPregeneratedTime = 0;
            return false;
        }

        public bool HasFreeRuneSpawn()
        {
            foreach (var a in runeSpawns)
                if (a.currentRune == null) return true;
            return false;
        }

        public RuneSpawn GetRandomRuneSpawn()
        {
            List<RuneSpawn> freeRuneSpawns = new List<RuneSpawn>();
            foreach (var a in runeSpawns)
                if (a.currentRune == null) freeRuneSpawns.Add(a);
            int randomNewRuneIndex = random.Next(0, freeRuneSpawns.Count);
            RuneSpawn chosenRuneSpawn = freeRuneSpawns[randomNewRuneIndex];
            freeRuneSpawns = null;
            return chosenRuneSpawn;
        }

        public Rune GetRandomRuneType() => (Rune)random.Next(1, 9);


        #endregion

        #region Client interaction with rune
        public void PlayerTriesToPickUpRune(int runeId, Player player)
        {
            var a = DoesRuneWithIdExists(runeId);
            if(a != null)
            {
                if(DoesClientCloseEnoughToTheRune(player.position, a))
                {
                    player.modifiersManager.AddRuneEffectOnPlayer(a.currentRune.rune);
                    NotifyAllPlayersOnPlayerPickingUpRune(runeId, player, a.currentRune.rune);
                    RunePickedUp_ResetRuneSpawn(a);
                }
            }
        }

        RuneSpawn DoesRuneWithIdExists(int runeId)
        {
            foreach (var a in runeSpawns)
                if (a.currentRune != null && a.currentRune.runeUniqueId == runeId) return a;
            return null;
        }

        bool DoesClientCloseEnoughToTheRune(Vector3 clientPos, RuneSpawn runeSpawnWithInstanceToCheck)
        {
            if (Vector3.Distance(clientPos, runeSpawnWithInstanceToCheck.position) < 6f) return true;
            return false;
        }

        void RunePickedUp_ResetRuneSpawn(RuneSpawn affectedRuneSpawn) => affectedRuneSpawn.currentRune = null;

        #endregion


        #region System classes
        public class RuneSpawn
        {
            public Vector3 position;
            public RuneInstance currentRune;

            public RuneSpawn(Vector3 position, RuneInstance currentRune)
            {
                this.position = position;
                this.currentRune = currentRune;
            }

            public string PositionToString()
            {
                return $"{position.X}/{position.Y}/{position.Z}";
            }
        }

        public class RuneInstance
        {
            public Rune rune;
            public int runeUniqueId;

            public RuneInstance(Rune rune, int runeUniqueId)
            {
                this.rune = rune;
                this.runeUniqueId = runeUniqueId;
            }
        }
        #endregion
    }
}
