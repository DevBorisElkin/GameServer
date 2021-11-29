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
            if (hasFreeRuneSpawn && hadNoFreeSpawns)
            {
                hadNoFreeSpawns = false;
                SetNextRuneSpawnTime();
                //Console.WriteLine("Free space for new rune has been found, resetting rune spawn cooldown and waiting for time to arrise");
                return;
            }
            else if (!hasFreeRuneSpawn)
            {
                hadNoFreeSpawns = true;
                return;
            }
            else if (hasFreeRuneSpawn && HasTimeReachedToSpawnNextRune(out float totalTimeSpentForRuneSpawn, out float totalNextRuneSpawnPregeneratedTime))
            {
                SetNextRuneSpawnTime();
                SpawnRune_FromRunesManager(totalTimeSpentForRuneSpawn, totalNextRuneSpawnPregeneratedTime);
            }
        }

        public void SpawnRune_FromRunesManager(float timeSpentForSpawn, float totalPregeneratedTime, Rune runeType = Rune.Random)
        {
            int newRuneId = GetUniqueNewRuneId();
            if (newRuneId == -1) return;

            RuneSpawn chosenRuneSpawn = GetRandomRuneSpawn();

            if (chosenRuneSpawn.currentRune != null)
            {
                Console.WriteLine($"[{DateTime.Now}][RUNE_MANAGER]: Can't spawn rune, no free rune spawns");
                return;
            }
            if (runeType == Rune.None)
            {
                Console.WriteLine($"[{DateTime.Now}][RUNE_MANAGER]: Can't spawn rune with rune type [{runeType}]");
                return;
            }

            Rune selectedRuneType;
            if (runeType == Rune.Random)
                selectedRuneType = GetRandomRuneType();
            else selectedRuneType = runeType;

            SpawnRune(chosenRuneSpawn, selectedRuneType, newRuneId, "none");

            if (timeSpentForSpawn == 0 && totalPregeneratedTime == 0)
                Console.WriteLine($"[{DateTime.Now}][Rune spawned]:[{chosenRuneSpawn.currentRune.rune}] by console command");
            else
                Console.WriteLine($"[{DateTime.Now}][Rune spawned]:[{chosenRuneSpawn.currentRune.rune}] totalSpawnTime: [{timeSpentForSpawn}], pregeneratedSpawnTime: [{totalPregeneratedTime}]");
        }

        public void SpawnRunes_AdminCommand(Player invoker, Rune runeType, CustomRuneSpawn_Amount amount, CustomRuneSpawn_Position position, bool notifyOthers)
        {
            int IntAmount;
            int freeSpawnsAmount = FreeSpawnsAmount();
            if (amount == CustomRuneSpawn_Amount.Max)
                IntAmount = FreeSpawnsAmount();
            else
            {
                switch (amount)
                {
                    case CustomRuneSpawn_Amount.One: IntAmount = 1; break;
                    case CustomRuneSpawn_Amount.Three: IntAmount = 3; break;
                    case CustomRuneSpawn_Amount.Five: IntAmount = 5; break;
                    default: IntAmount = 1; break;
                }
            }
            if (IntAmount > freeSpawnsAmount) IntAmount = freeSpawnsAmount;
            if (IntAmount == 0) { Console.WriteLine($"[{DateTime.Now}][AdminCommands]: Can't spawn rune, no free spawns, IntAmount[{IntAmount}] freeSpawnsAmount[{freeSpawnsAmount}]"); return; }

            List<RuneSpawn> selectedRuneSpawns;
            if(position == CustomRuneSpawn_Position.ClosestSpawn)
                selectedRuneSpawns = GetClosestFreeRuneSpawnsToPlayer(invoker);
            else selectedRuneSpawns = GetRandomFreeRuneSpawns();

            if(selectedRuneSpawns.Count < IntAmount) { IntAmount = selectedRuneSpawns.Count; }
            if (IntAmount == 0) { Console.WriteLine($"[{DateTime.Now}][AdminCommands]: Can't spawn rune, no free spawns, IntAmount[{IntAmount}] selectedRuneSpawns.Count[{selectedRuneSpawns.Count}]"); return; }

            string dbIdOfInvoker = "none";
            if (notifyOthers) dbIdOfInvoker = invoker.client.userData.db_id.ToString();

            for (int i = 0; i < IntAmount; i++)
            {
                Rune selectedRuneType;
                if (runeType == Rune.None) selectedRuneType = GetRandomRuneType();
                else selectedRuneType = runeType;

                int newRuneId = GetUniqueNewRuneId();
                if (newRuneId == -1) return;

                SpawnRune(selectedRuneSpawns[i], selectedRuneType, newRuneId, dbIdOfInvoker);
            }
        }

        void SpawnRune(RuneSpawn runeSpawn, Rune selectedRuneType, int newRuneId, string dbIdOfInvoker = "none")
        {
            runeSpawn.currentRune = new RuneInstance(selectedRuneType, newRuneId);
            NotifyAllPlayersOnNewSpawnedRune(assignedPlayroom, runeSpawn.position, runeSpawn.currentRune.rune, newRuneId, dbIdOfInvoker);
            if(!dbIdOfInvoker.Equals("none"))
                Console.WriteLine($"[{DateTime.Now}][Rune spawned by Admin command]:[{runeSpawn.currentRune.rune}]");
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
                timeSpentForRuneSpawn = (float)TimeSpan.FromMilliseconds(msSinceNextSpawnInceptionTime).TotalSeconds;
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
        public int FreeSpawnsAmount()
        {
            int amount = 0;
            foreach (var a in runeSpawns)
                if (a.currentRune == null) amount++;
            return amount;
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

        public List<RuneSpawn> GetClosestFreeRuneSpawnsToPlayer(Player invoker)
        {
            List<RuneSpawn> freeRuneSpawns = new List<RuneSpawn>();
            foreach (var a in runeSpawns) if (a.currentRune == null) freeRuneSpawns.Add(a);
            List<RuneSpawn> closestRuneSpawns = freeRuneSpawns.OrderBy(item => Vector3.Distance(invoker.position, item.position)).ToList();
            return closestRuneSpawns;
        }
        public List<RuneSpawn> GetRandomFreeRuneSpawns()
        {
            List<RuneSpawn> freeRuneSpawns = new List<RuneSpawn>();
            foreach (var a in runeSpawns) if (a.currentRune == null) freeRuneSpawns.Add(a);
            return freeRuneSpawns;
        }
        public Rune GetRandomRuneType() => (Rune)random.Next(1, 9);


        #endregion

        #region Client interaction with rune
        public void PlayerTriesToPickUpRune(int runeId, Player player)
        {
            var a = DoesRuneWithIdExists(runeId);
            if (a != null)
            {
                if (DoesClientCloseEnoughToTheRune(player.position, a))
                {
                    AddRandomJumpsAmount_RuneReward(player);

                    player.modifiersManager.AddRuneEffectOnPlayer(a.currentRune.rune);
                    NotifyAllPlayersOnPlayerPickingUpRune(runeId, player, a.currentRune.rune);
                    RunePickedUp_ResetRuneSpawn(a);
                }
            }
        }

        void AddRandomJumpsAmount_RuneReward(Player player)
        {
            int chanceToFillMax = random.Next(1, 101);
            if (chanceToFillMax > 10)
            {
                int randomAmountOfBonusJumps = random.Next(PlayroomManager.minRandomAmountOfRuneJumps, PlayroomManager.maxRandomAmountOfRuneJumps + 1);
                player.CheckAndAddJumps(randomAmountOfBonusJumps, true);

            }
            else player.CheckAndAddJumps(PlayroomManager.maxJumpsAmount, true);

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
