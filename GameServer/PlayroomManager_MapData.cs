using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public static class PlayroomManager_MapData
    {
        #region Additional data

        public enum Map { DefaultMap }
        public enum MatchState { WaitingForPlayers, InGame, Finished }
        public enum MatchResult { PlayerWon, Draw, Discarded}
        public enum MatchFinishReason { FinishedByKills, FinishedByTime, Discarded }

        public static Vector3 GetRandomSpawnPointByMap(Map _map)
        {
            foreach(MapData data in mapDatas)
            {
                if (data.map.Equals(_map))
                {
                    return data.mapSpawns[random.Next(0, data.mapSpawns.Count)];
                }
            }
            Console.WriteLine($"[{DateTime.Now}] Error, couldn't get random spawn coordinates for player!");
            return Vector3.Zero;
        }

        static int spawnPointsToConsider = 3;

        static Random random = new Random();
        public static Vector3 GetRandomSpawnPointByMap_FarthestPos(Map _map, Playroom activePlayroom, Player playerToExclude)
        {
            if (activePlayroom.playersInPlayroom.Count == 1) return GetRandomSpawnPointByMap(_map);

            MapData correctMapData = null;
            foreach (MapData data in mapDatas) if (data.map.Equals(_map)) correctMapData = data;
            if (correctMapData == null) { Console.WriteLine($"[SERVER_ERROR]: couldn't get correct map data for map {_map}"); return GetRandomSpawnPointByMap(_map); }

            List<MapCalculation> calculations = new List<MapCalculation>();
            foreach(var a in correctMapData.mapSpawns)
            {
                float totalDistToAllPlayers = 0;
                float calculatedAvgPos = 0;
                for (int i = 0; i < activePlayroom.playersInPlayroom.Count; i++)
                {
                    if (activePlayroom.playersInPlayroom[i] == playerToExclude) continue;
                    totalDistToAllPlayers += Vector3.Distance(a, activePlayroom.playersInPlayroom[i].position);
                }
                calculatedAvgPos = totalDistToAllPlayers / (activePlayroom.playersInPlayroom.Count - 1);
                calculations.Add(new MapCalculation(a, calculatedAvgPos));
            }

            calculations.Sort((a, b) => b.totalAvgPosToAllPlayers.CompareTo(a.totalAvgPosToAllPlayers));
            //foreach (var a in calculations) Console.WriteLine($"{a.totalAvgPosToAllPlayers}");

            // either spawns at furthest position from other players or at the middle position
            // 70% chance that it will take farthest position
            int randomDistChoice = random.Next(0, 10);
            if(randomDistChoice < 6)
            {
                int randomSelectedRandomRemotePos = random.Next(0, spawnPointsToConsider);
                //Console.WriteLine("Generated random spawn pos: " + randomSelectedRandomRemotePos);
                return calculations[randomSelectedRandomRemotePos].spawnPos;
            }
            else
            {
                int randomMiddleIndex = calculations.Count / 2;
                int randomFinalIndex = randomMiddleIndex += random.Next(-1, 2);
                return calculations[randomFinalIndex].spawnPos;
            }
        }

        // will remember last 3 used spawn positions by playroom and will try not to use them
        // work in progress
        /*
        public static Vector3 GetRandomSpawnPointByMap_UnrepeatablePosition(Map _map)
        {
            foreach (MapData data in mapDatas)
            {
                if (data.map.Equals(_map))
                {
                    Random random = new Random();
                    return data.mapSpawns[random.Next(0, data.mapSpawns.Count)];
                }
            }
            Console.WriteLine($"[{DateTime.Now}] Error, couldn't get random spawn coordinates for player!");
            return Vector3.Zero;
        }
        */

        public static List<MapData> mapDatas = new List<MapData>
        { new MapData(Map.DefaultMap, new List<Vector3> {
            new Vector3(15.66f, 14.99f, -3.22f),
            new Vector3(-50.3f, 15.052f, -43.84f),
            new Vector3(-48.94f, 14.958f, -10.74f),
            new Vector3(9.22f, 15.008f, -35f),
            new Vector3(-35.16f, 20.998f, -4.9f),
            new Vector3(-7.66f, 20.984f, -42.08f),
            new Vector3(-27.8f, 14.982f, -13.96f),
            new Vector3(-3.16f, 14.96f, -24.26f)
        }) };

        public class MapData
        {
            public Map map;
            public List<Vector3> mapSpawns;
            public MapData(Map _map, List<Vector3> _spawnPoints)
            {
                map = _map;
                mapSpawns = _spawnPoints;
            }
        }

        public class MapCalculation
        {
            public Vector3 spawnPos;
            public float totalAvgPosToAllPlayers;

            public MapCalculation(Vector3 spawnPos, float totalAvgPosToAllPlayers)
            {
                this.spawnPos = spawnPos;
                this.totalAvgPosToAllPlayers = totalAvgPosToAllPlayers;
            }
        }

        #endregion
    }
}
