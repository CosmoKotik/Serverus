using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    internal class Config
    {
        private const string _configPath = @"config.cfg";
        private static Dictionary<string, string> _configVariables = new Dictionary<string, string>();

        public static void LoadConfigs()
        {
            using (FileStream fileStream = File.OpenRead(_configPath))
            {
                using (StreamReader stream = new StreamReader(fileStream, Encoding.UTF8, true, 128))
                {
                    string? line;
                    while ((line = stream.ReadLine()) != null)
                    {
                        //Checks for commentations
                        for (int i = 0; i < line.Length; i++)
                            if (line[i].Equals("#"))
                            {
                                line = line.Substring(i - 1);
                                break;
                            }

                        string variable = line.Split('=')[0].Trim();
                        string value = line.Split('=')[1].Trim();

                        _configVariables.Add(variable, value);
                    }
                }
            }
        }

        public static string GetConfig(string variable)
        {
            return _configVariables.GetValueOrDefault(variable)!;
        }
    }
}
