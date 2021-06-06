using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Serverus.ACS
{
    public class LoadMap
    {
        public Map[] load(int mapId)
        {
            string path = @"/Maps/";

            Map[] mapArray = null;

            string[] mapData = File.ReadAllLines(path);

            for (int i = 0; i < mapData.Length; i++)
            {
                mapArray[i] = JsonConvert.DeserializeObject<Map>(mapData[i]);
            }

            return mapArray;
        }
    }
}
