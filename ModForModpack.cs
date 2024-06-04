using System.IO;

namespace STTUMM
{
    public class ModForModpack
    {
        public ModData.ModBase ModData { get; set; }
        public string ModName { get; set; }
        public string ModPath { get; set; }
        public ModForModpack(ModData.ModBase data, string path)
        {
            ModData = data;
            ModName = $"{data.name} v{data.version}";
            ModPath = path;
        }
    }
}
