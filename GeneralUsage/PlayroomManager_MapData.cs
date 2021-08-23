using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GeneralUsage
{
    public static class PlayroomManager_MapData
    {
        #region Additional data

        public enum Map { DefaultMap }

        public static Vector3 GetRandomSpawnPointByMap(Map _map)
        {
            foreach(MapData data in mapDatas)
            {
                if (data.map.Equals(_map))
                {
                    Random random = new Random();
                    return data.mapSpawns[random.Next(0, data.mapSpawns.Count)];
                }
            }
            Console.WriteLine("Error, couldn't get random spawn coordinates for player!");
            return Vector3.Zero;
        }

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

        #endregion
    }
}
