using Newtonsoft.Json;
using System.IO;

namespace STTUMM.ModData
{
    public class SkylanderMod : ModBase
    {
        public Skylander skylander { get; set; }

        public SkylanderMod() : base()
        {
            skylander = new Skylander();
            type = "skylander";
        }
        public static new SkylanderMod Load(string json)
        {
            return JsonConvert.DeserializeObject<SkylanderMod>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    public class Skylander
    {
        public string name { get; set; }
        public string name_bot { get; set; }
        public string element { get; set; }
        public string game { get; set; }
        public string type { get; set; }
        public Variants variants { get; set; }
        public Skylander()
        {
            name = "";
            name_bot = "";
            element = "magic";
            game = "ssa";
            type = "";
            variants = new Variants();
        }
    }
    public class Variants
    {
        public bool normal { get; set; }
        public bool s2 { get; set; }
        public bool s3 { get; set; }
        public bool s4 { get; set; }
        public bool lightcore { get; set; }
        public bool eon { get; set; }
        public Variants()
        {
            normal = true;
            s2 = false;
            s3 = false;
            s4 = false;
            lightcore = false;
            eon = false;
        }
    }
}
