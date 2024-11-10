using Newtonsoft.Json;
using System.IO;

namespace STTUMM
{
    public class Config
    {
        public Paths Paths { get; set; }
        public string Region { get; set; }

        public Config()
        {
            Paths = new Paths();
            Region = "USA";
        }

        public static Config Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Config>(json);
            }
            else
            {
                return new Config();
            }
        }

        public void Save(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
    public class Paths
    {
        public string Loadiine { get; set; }
        public string Cemu { get; set; }
        public string Dump { get; set; }
        public Paths()
        {
            Loadiine = string.Empty;
            Cemu = string.Empty;
            Dump = string.Empty;
        }
    }
}
