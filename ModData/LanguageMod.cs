using Newtonsoft.Json;

namespace STTUMM.ModData
{
    public class LanguageMod : ModBase
    {

        public LanguageMod() : base()
        {
            type = "language";
        }
        public static new LanguageMod Load(string json)
        {
            return JsonConvert.DeserializeObject<LanguageMod>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
