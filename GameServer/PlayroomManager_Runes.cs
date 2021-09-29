using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ServerCore;

namespace GameServer
{
    public class PlayroomManager_Runes
    {
        Playroom assignedPlayroom;
        public void Init(Playroom playroom)
        {

        }
        public void SpawnRune()
        {

        }

        public int GetUniqueNewRuneId()
        {
            return -1;
        }
        




        public class RuneSpawn
        {
            public Vector3 position;
            public RuneInstance currentRune;
        }

        public class RuneInstance
        {
            public Rune rune;
            public int runeUniqueId;
        }
    }
}
