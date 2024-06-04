using Newtonsoft.Json;

namespace STTUMM.ModData
{
    public class ModBase
    {
        public string name { get; set; }
        public string author { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public ModBase()
        {
            name = "";
            author = "";
            version = "1.0";
            description = "description";
            type = "";
        }
        public static ModBase Load(string json)
        {
            return JsonConvert.DeserializeObject<ModBase>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
