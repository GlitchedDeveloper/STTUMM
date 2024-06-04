using System.Collections.ObjectModel;
using System.IO;

namespace STTUMM
{
    public class ModpackListItem : ModListItem
    {
        public ObservableCollection<ModListItem> ModpackMods { get; set; }
        public ModpackListItem(string dir) : base(dir)
        {
            ModpackMods = new ObservableCollection<ModListItem>();
        }
    }
}
