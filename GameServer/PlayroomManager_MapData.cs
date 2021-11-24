using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ServerCore.ModifiersManager;

namespace ServerCore
{
    public static class PlayroomManager_MapData
    {
        #region Additional data

        public enum Map { DefaultMap }
        public enum MatchState { WaitingForPlayers, InGame, Finished, JustStarting }
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

        // tries to retrieve the farthest position from all existing players
        // if there's 1 player, returns random spawn position
        // if there's 2-3 players, tries to get middle spawn point
        // 'player to exclude' is to for whom we're getting new spawn position, so he'll be excluded from calculation
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
            int randomDistChoice = random.Next(0, 100);

            if(activePlayroom.playersInPlayroom.Count > 3)
            {
                if (randomDistChoice < 70)
                {
                    int randomSelectedRandomRemotePos = random.Next(0, spawnPointsToConsider);
                    return calculations[randomSelectedRandomRemotePos].spawnPos;
                }
                else
                {
                    int randomMiddleIndex = calculations.Count / 2;
                    int randomFinalIndex = randomMiddleIndex += random.Next(-2, 3);
                    return calculations[randomFinalIndex].spawnPos;
                }
            }
            else
            {
                if (randomDistChoice < 20)
                {
                    int randomSelectedRandomRemotePos = random.Next(0, spawnPointsToConsider);
                    return calculations[randomSelectedRandomRemotePos].spawnPos;
                }
                else
                {
                    int randomMiddleIndex = calculations.Count / 2;
                    int randomFinalIndex = randomMiddleIndex += random.Next(-2, 3);
                    return calculations[randomFinalIndex].spawnPos;
                }
            }
        }
        // tries to get random (not the same) spawn positions for all clients
        public static Vector3[] GetRandomSpawnPointByMap_UnrepeatablePosition(Map _map, int clientsAmount, out bool useRandomPositions)
        {
            MapData correctMapData = null;
            foreach (MapData data in mapDatas) if (data.map.Equals(_map)) correctMapData = data;
            if (correctMapData == null) 
            {
                useRandomPositions = true;
                Console.WriteLine($"[SERVER_ERROR]: couldn't get correct map data for map #2 {_map}"); return correctMapData.mapSpawns.ToArray(); 
            }

            List<MapRandomPos> mapRandomPositions = new List<MapRandomPos>();
            Vector3[] mapRandPos;
            foreach (var a in correctMapData.mapSpawns) mapRandomPositions.Add(new MapRandomPos(a, false));

            if(clientsAmount <= correctMapData.mapSpawns.Count)
            {
                mapRandPos = new Vector3[clientsAmount];
                for (int i = 0; i < mapRandPos.Length; i++)
                {
                    MapRandomPos chosenPos;
                    int glitchDetection = 0;
                    while (true)
                    {
                        int randPosIndex = random.Next(0, mapRandomPositions.Count);
                        if (!mapRandomPositions[randPosIndex].alreadyTaken)
                        {
                            chosenPos = mapRandomPositions[randPosIndex];
                            chosenPos.alreadyTaken = true;
                            break;
                        }
                        glitchDetection++;
                        if(glitchDetection > 200)
                        {
                            Console.WriteLine("While(true) loop glitched. breaking out");
                            useRandomPositions = true;
                            return correctMapData.mapSpawns.ToArray();
                        }
                    }
                    mapRandPos[i] = chosenPos.spawnPos;
                }
                useRandomPositions = false;
                return mapRandPos;
            }
            else
            {
                mapRandPos = new Vector3[correctMapData.mapSpawns.Count];
                useRandomPositions = true;
                // there are more clients than spawn positions, in that case returning totally random spawn positions
                for (int i = 0; i < mapRandPos.Length; i++)
                    mapRandPos[i] = correctMapData.mapSpawns[i];
                return mapRandPos;
            }
        }
        
        public static MapData GetMapDataByMap(Map map)
        {
            MapData correctMapData = null;
            foreach (MapData data in mapDatas) if (data.map.Equals(map)) correctMapData = data;
            if (correctMapData == null)
            {
                Console.WriteLine($"[SERVER_ERROR]: couldn't get correct map data for map #2 {map}"); return null;
            }
            return correctMapData;
        }

        public static List<MapData> mapDatas = new List<MapData>
        { new MapData(
            Map.DefaultMap, 
            new List<Vector3> {
                new Vector3(15.66f, 14.99f, -3.22f),
                new Vector3(-50.3f, 15.052f, -43.84f),
                new Vector3(-48.94f, 14.958f, -10.74f),
                new Vector3(9.22f, 15.008f, -35f),
                new Vector3(-35.16f, 20.998f, -4.9f),
                new Vector3(-7.66f, 20.984f, -42.08f),
                new Vector3(-27.8f, 14.982f, -13.96f),
                new Vector3(-3.16f, 14.96f, -24.26f),
                new Vector3(-85.264f, 14.96f, -48.7f),
                new Vector3(-101.88f, 14.96f, -15.4f),
                new Vector3(-94.74f, 14.96f, 2.78f),
                new Vector3(-2.56f, 14.96f, 38.62f),
                new Vector3(-12.86f, 14.96f, -74.24f),
                new Vector3(41.1f, 14.96f, -35.24f),
                new Vector3(74.92f, 20.942f, -26.1f),
                new Vector3(-106.6f, 14.96f, -35.26f),
                new Vector3(-12.72f, 14.96f, -48.06f),
                new Vector3(55.12f, 14.96f, -4.92f),
                new Vector3(-26.66f, 14.96f, 19f),
                new Vector3(61.64f, 14.96f, -21.1f)},
            new List<Vector3>
            {
                new Vector3(-92.26f, 14.99f, -24.54f),
                new Vector3(-19.62f, 18.78f, -23.26f),
                new Vector3(-61.10f, 14.99f, -22.98f),
                new Vector3(20.908f, 14.99f, -21.4f),
                new Vector3(16.58f, 14.99f, 32.9f),
                new Vector3(73.268f, 14.99f, -21.00f),
                new Vector3(57.532f, 14.99f, 20.416f),
                new Vector3(41.16f, 21.292f, -42.96f),
                new Vector3(-23.14f, 23.06f, -43.908f),
                new Vector3(-118.84f, 25.37f, -5.414f),
                new Vector3(-81.5f, 21.292f, 10.688f),
                new Vector3(-84.7f, 21.292f, 10.688f),
                new Vector3(-13.114f, 18.71f, -67.08f),
            }
            
            ) };

        public class MapData
        {
            public Map map;
            public List<Vector3> mapSpawns;
            public List<Vector3> runeSpawns;
            public MapData(Map _map, List<Vector3> _spawnPoints, List<Vector3> _runeSpawns)
            {
                map = _map;
                mapSpawns = _spawnPoints;
                runeSpawns = _runeSpawns;
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

        public class MapRandomPos
        {
            public Vector3 spawnPos;
            public bool alreadyTaken;

            public MapRandomPos(Vector3 spawnPos, bool alreadyTaken)
            {
                this.spawnPos = spawnPos;
                this.alreadyTaken = alreadyTaken;
            }
        }

        #endregion
    }
}
