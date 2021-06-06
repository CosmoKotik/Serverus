using System;
using System.Collections.Generic;
using System.Text;
using Serverus.Api;

namespace Serverus.ACS
{
    public class AntiCheat
    {
        private Map[] map;
        public AntiCheat(int mapId)
        {
            LoadMap loader = new LoadMap();

            map = loader.load(mapId);
        }

        public bool CheckCheats(Player player)
        {
            for (int i = 0; i < map.Length; i++)
            {
                float xStart = map[i].Collision.X;
                float xEnd = xStart * 2;

                float yStart = map[i].Collision.Y;
                float yEnd = yStart * 2;

                float zStart = map[i].Collision.Z;
                float zEnd = zStart * 2;

                if (player.Pos.X > xStart && player.Pos.X < xEnd &&
                    player.Pos.Y > yStart && player.Pos.Y < yEnd &&
                    player.Pos.Z > zStart && player.Pos.Z < zEnd)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
