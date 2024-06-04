using Newtonsoft.Json;

namespace STTUMM.ModData
{
    public class Modpack : ModBase
    {

        public Modpack() : base()
        {
            type = "modpack";
        }
        public static new Modpack Load(string json)
        {
            return JsonConvert.DeserializeObject<Modpack>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
