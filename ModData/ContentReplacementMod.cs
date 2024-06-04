using Newtonsoft.Json;

namespace STTUMM.ModData
{
    public class ContentReplacementMod : ModBase
    {

        public ContentReplacementMod() : base()
        {
            type = "content";
        }
        public static new ContentReplacementMod Load(string json)
        {
            return JsonConvert.DeserializeObject<ContentReplacementMod>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
