using System.IO;

namespace STTUMM
{
    public class ModListItem
    {
        public bool Enabled { get; set; }
        public string ModName { get; set; }
        public string ModDirectory { get; set; }
        public ModListItem(string dir)
        {
            var data = ModData.ModBase.Load(File.ReadAllText(Path.Combine(dir, "data.json")));
            Enabled = !File.Exists(Path.Combine(dir, "disabled"));
            ModName = $"{data.name} v{data.version}";
            ModDirectory = dir;
        }
    }
}
