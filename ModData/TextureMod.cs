using Newtonsoft.Json;

namespace STTUMM.ModData
{
    public class TextureMod : ModBase
    {

        public TextureMod() : base()
        {
            type = "texture";
        }
        public static new TextureMod Load(string json)
        {
            return JsonConvert.DeserializeObject<TextureMod>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
