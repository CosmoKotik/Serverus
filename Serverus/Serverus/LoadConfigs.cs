using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Git_Git_Server
{
    public class LoadConfigs
    {
        public string LocalConfigPath = @"Configs\local_config.conf";
        public List<string> Configs = new List<string>();

        public void Load_Local()
        {
            if (File.Exists(LocalConfigPath))
            {
                using (StreamReader sr = new StreamReader(LocalConfigPath))
                {
                    while (!sr.EndOfStream)
                    {
                        Configs.Add(sr.ReadLine());
                    }
                    Console.WriteLine("Successfuly loaded");
                }
            }
            else
                Console.WriteLine("Error 1: Config file missing");
        }

        public string GetLocalConfig(string config)
        {
            int index = Configs.FindIndex(conf => conf.Contains(config));
            bool isAfterEqual = false;
            string result = "";

            for (int i = 0; i < Configs[index].Length; i++)
            {
                if (Configs[index][i] == '=' || isAfterEqual)
                {
                    isAfterEqual = true;
                    if (Configs[index][i] != '=' && Configs[index][i] != ' ')
                        result += Configs[index][i];
                }
            }

            return result;
        }
    }
}
