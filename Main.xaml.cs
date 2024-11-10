using Newtonsoft.Json;
using STTUMM.IGAE;
using STTUMM.Tools;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.IO.Compression;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Xml.Linq;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Navigation;
using STTUMM.IGAE.Types;
using System.Text;
using System.Reflection;
using System.Windows.Threading;
using System.IO.Pipes;
using System.Windows.Shell;

namespace STTUMM
{
    public partial class Main : Window
    {
        public Config config;
        private string configPath;
        public int ActiveMenuIdx;
        private FrameworkElement[] Menus;
        public int ActiveModCreationMenuIdx;
        private FrameworkElement[] ModCreationMenus;
        private List<ModListItem> ContentMods;
        private List<ModListItem> SkylanderMods;
        private List<ModListItem> TextureMods;
        private List<ModListItem> LanguageMods;
        public Main(Config settings)
        {
            config = settings;
            configPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            InitializeComponent();
            AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(HandleRequestNavigate));
            LoadiineFolderSelect.SetPath(config.Paths.Loadiine);
            CEMUFolderSelect.SetPath(config.Paths.Cemu);
            DumpFolderSelect.SetPath(config.Paths.Dump);
            if (config.Region == "EUR")
            {
                GameRegion.SelectedIndex = 1;
            }
            else
            {
                GameRegion.SelectedIndex = 0;
            }
            Menus = new FrameworkElement[] {
                ModsMenu,
                CreateMenu,
                SettingsMenu,
                CreditsMenu,
                CEMUMenu
            };
            ActiveMenuIdx = 0;
            ModCreationMenus = new FrameworkElement[] {
                CreateModDetails,
                CreateSkylanderModDetails,
                CreateContentReplacementModDetails,
                CreateTextureModDetails,
                CreateLanguageModDetails,
                CreateModpackDetails
            };
            ActiveModCreationMenuIdx = 0;
            ModsMenu.Height = 880;
            CreateMenu.Height = 880;
            SettingsMenu.Height = 880;
            CreditsMenu.Height = 880;
            this.SizeChanged += OnSizeChanged;
            ContentMods = new List<ModListItem>();
            SkylanderMods = new List<ModListItem>();
            TextureMods = new List<ModListItem>();
            LanguageMods = new List<ModListItem>();
            if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Installed Mods")))
                Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Installed Mods"));
            if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups")))
                Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups"));
            if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Skylander Backups")))
                Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Skylander Backups"));
            UpdateModData();
            Task.Run(Server);
            //byte[] dump = SkylanderDumps.Generate(453, 14341);
            //File.WriteAllBytes(Path.Join(config.Paths.Dump, $"TEST [453].sky"), dump);
            //dump = SkylanderDumps.Generate(453, 15362);
            //File.WriteAllBytes(Path.Join(config.Paths.Dump, $"TEST [453] 2.sky"), dump);
            if (File.Exists(Path.Join(config.Paths.Cemu, "Cemu.exe")))
            {
                CEMUMenuButton.Visibility = Visibility.Visible;
                UpdateAccounts();
                UpdateSaveData();
            }
            else
            {
                CEMUMenuButton.Visibility = Visibility.Collapsed;
            }
        }

        string SanitizeFolderName(string folderName)
        {
            return Regex.Replace(folderName, @"[\\/:*?""<>|]", "");
        }
        public bool IsDiff(Stream stream1, Stream stream2)
        {
            stream1.Seek(0, SeekOrigin.Begin);
            stream2.Seek(0, SeekOrigin.Begin);
            if (stream1.Length != stream2.Length)
                return true;
            while (true)
            {
                int byte1 = stream1.ReadByte();
                int byte2 = stream2.ReadByte();
                if (byte1 != byte2)
                    return true;
                if (byte1 == -1)
                    return false;
            }
        }
        public bool IsDiff(string filePath1, string filePath2)
        {
            using (var stream1 = new FileStream(filePath1, FileMode.Open))
            using (var stream2 = new FileStream(filePath2, FileMode.Open))
                return IsDiff(stream1, stream2);
        }
        public bool IsDiff(string filePath, Stream stream)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open))
                return IsDiff(fileStream, stream);
        }
        private void HandleRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var uri = e.Uri;
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ModsMenu.Height = e.NewSize.Height - 20;
            CreateMenu.Height = e.NewSize.Height - 20;
            SettingsMenu.Height = e.NewSize.Height - 20;
            CreditsMenu.Height = e.NewSize.Height - 20;
        }
        private string CreateGraphicPack()
        {
            string graphicPackDir = Path.Join(config.Paths.Cemu, "graphicPacks", "STTUMM");
            if (Directory.Exists(graphicPackDir))
            {
                Directory.Delete(graphicPackDir, true);
            }
            Directory.CreateDirectory(graphicPackDir);
            File.WriteAllText(Path.Join(config.Paths.Cemu, "graphicPacks", "STTUMM", "rules.txt"), "[Definition]\ntitleIds = 000500001017C600,0005000010181F00\nname = STTUMM\npath = \"Skylanders Trap Team/STTUMM\"\ndescription = Mods Merged By STTUMM\nversion = 5");
            XDocument doc = XDocument.Load(Path.Join(config.Paths.Cemu, "settings.xml"));
            XElement graphicPack = doc.Element("content").Element("GraphicPack");
            bool isGraphicsPackIncluded = graphicPack.Elements("Entry").Any(e => (string)e.Attribute("filename") == "graphicPacks/STTUMM/rules.txt");
            if (!isGraphicsPackIncluded)
            {
                XElement entry = new XElement("Entry");
                entry.SetAttributeValue("filename", "graphicPacks/STTUMM/rules.txt");
                graphicPack.Add(entry);
                doc.Save(Path.Join(config.Paths.Cemu, "settings.xml"));
            }
            return graphicPackDir;
        }

        private void GenerateSkylanderDump(ModListItem mod, ModData.SkylanderMod data, string ModID, Dictionary<string, int> modToID, Dictionary<string, byte[]> oldModToDump, int variantId, string variant = "")
        {
            string path = config.Paths.Dump;
            if (mod.ModpackName != null)
                path = Path.Join(path, SanitizeFolderName(mod.ModpackName), SanitizeFolderName(mod.ModName));
            else
                path = Path.Join(path, SanitizeFolderName(mod.ModName));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            byte[] dump;
            if (data.skylander.type == "swap")
            {
                if (oldModToDump.ContainsKey($"{ModID}-{variantId}"))
                {
                    dump = SkylanderDumps.SetID(oldModToDump[$"{ModID}-{variantId}"], modToID[ModID]);
                }
                else
                {
                    dump = SkylanderDumps.Generate(modToID[ModID], variantId);
                }
                if (variant == "")
                {
                    File.WriteAllBytes(Path.Join(path, $"{FilterFilename(data.skylander.name)} (Top) [{modToID[ModID]}].sky"), dump);
                }
                else
                {
                    File.WriteAllBytes(Path.Join(path, $"{FilterFilename(data.skylander.name)} ({variant}) (Top) [{modToID[ModID]}].sky"), dump);
                }

                if (oldModToDump.ContainsKey($"{ModID}-bot-{variantId}"))
                {
                    dump = SkylanderDumps.SetID(oldModToDump[$"{ModID}-bot-{variantId}"], modToID[$"{ModID}-bot"]);
                }
                else
                {
                    dump = SkylanderDumps.Generate(modToID[$"{ModID}-bot"], variantId);
                }
                if (variant == "")
                {
                    File.WriteAllBytes(Path.Join(path, $"{FilterFilename(data.skylander.name_bot)} (Bottom) [{modToID[$"{ModID}-bot"]}].sky"), dump);
                }
                else
                {
                    File.WriteAllBytes(Path.Join(path, $"{FilterFilename(data.skylander.name_bot)} ({variant}) (Bottom) [{modToID[$"{ModID}-bot"]}].sky"), dump);
                }
            }
            else
            {
                if (oldModToDump.ContainsKey($"{ModID}-{variantId}"))
                {
                    dump = SkylanderDumps.SetID(oldModToDump[$"{ModID}-{variantId}"], modToID[ModID]);
                }
                else
                {
                    dump = SkylanderDumps.Generate(modToID[ModID], variantId);
                }
                if (variant == "")
                {
                    Console.WriteLine($"{modToID[ModID]}:{variantId}");
                    File.WriteAllBytes(Path.Join(path, $"{FilterFilename(data.skylander.name)} [{modToID[ModID]}].sky"), dump);
                }
                else
                {
                    File.WriteAllBytes(Path.Join(path, $"{FilterFilename(data.skylander.name)} ({variant}) [{modToID[ModID]}].sky"), dump);
                }
            }
        }
        public static string GenerateInternalName(int num)
        {
            StringBuilder result = new StringBuilder();
            do
            {
                result.Insert(0, (char)('A' + num % 26));
                num /= 26;
            } while (num-- > 0);

            return result.ToString();
        }
        public async Task Server()
        {
            while (true)
            {
                using (var server = new NamedPipeServerStream("STTUMM_Pipe"))
                {
                    await server.WaitForConnectionAsync();
                    using (var reader = new StreamReader(server))
                    {
                        string message = await reader.ReadLineAsync();
                        if (message == "Reload Mods")
                        {
                            Dispatcher.Invoke(() => UpdateModData());
                        }
                    }
                }
            }
        }
        public async Task MergeWithDialog()
        {
            var d = new ProgressDialog("Processing...");
            d.Owner = this;
            var mergeTask = Task.Run(() => Merge(d));
            d.ShowDialog();
            await mergeTask;
        }
        public void Merge(ProgressDialog d)
        {
            if (Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content")))
            {
                Directory.Delete(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content"), true);
            }
            Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content"));
            Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character"));
            string graphicPackDir = null;
            if (config.Paths.Cemu != "")
            {
                graphicPackDir = CreateGraphicPack();
            }

            Dispatcher.Invoke(() => d.UpdateProgress("Merging Content Mods..."));
            foreach (ModListItem mod in ContentMods)
            {
                ModData.ContentReplacementMod data = ModData.ContentReplacementMod.Load(File.ReadAllText(Path.Join(mod.ModDirectory, "data.json")));
                foreach (string folderPath in Directory.GetDirectories(Path.Join(mod.ModDirectory, "content")))
                {
                    string folder = Path.GetFileName(folderPath);
                    if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder)))
                        Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder));
                    foreach (string filePath in Directory.GetFiles(folderPath))
                    {
                        string file = Path.GetFileName(filePath);
                        File.Copy(filePath, Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file), true);
                    }
                    foreach (string fileFolderPath in Directory.GetDirectories(folderPath))
                    {
                        string file = Path.GetFileName(fileFolderPath);
                        string igaPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file);
                        if (!File.Exists(igaPath))
                            igaPath = Path.Join(config.Paths.Loadiine, "content", folder, file);
                        IGA_File iga = new IGA_File(igaPath, IGA_Version.SkylandersTrapTeam);
                        Stream[] fileData = new Stream[iga.numberOfFiles];
                        if (file.EndsWith(".bld"))
                        {
                            for (uint i = 0; i < iga.numberOfFiles; i++)
                            {
                                if (File.Exists(Path.Join(fileFolderPath, iga.names[i])))
                                {
                                    var stream = new FileStream(Path.Join(fileFolderPath, iga.names[i]), FileMode.Open);
                                    stream.Seek(0, SeekOrigin.Begin);
                                    fileData[i] = stream;
                                }
                                else
                                {
                                    MemoryStream stream = new MemoryStream();
                                    iga.ExtractFile(i, stream, out _, true);
                                    stream.Seek(0, SeekOrigin.Begin);
                                    fileData[i] = stream;
                                }
                            }
                            iga.Build(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file), fileData);
                            iga.Close();
                        }
                        if (file.EndsWith(".arc"))
                        {
                            Dictionary<string, int> pathToIdx = new Dictionary<string, int>();
                            foreach (string path in Directory.GetFiles(fileFolderPath))
                                if (path.EndsWith(".path"))
                                    pathToIdx[File.ReadAllText(path)] = int.Parse(Path.GetFileNameWithoutExtension(path));

                            for (uint i = 0; i < iga.numberOfFiles; i++)
                            {
                                if (pathToIdx.ContainsKey(iga.names[i]))
                                {
                                    var stream = new FileStream(Path.Join(fileFolderPath, $"{pathToIdx[iga.names[i]]}.bin"), FileMode.Open);
                                    stream.Seek(0, SeekOrigin.Begin);
                                    fileData[i] = stream;
                                }
                                else
                                {
                                    MemoryStream stream = new MemoryStream();
                                    iga.ExtractFile(i, stream, out _, true);
                                    stream.Seek(0, SeekOrigin.Begin);
                                    fileData[i] = stream;
                                }
                            }
                            iga.Build(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file), fileData);
                            iga.Close();
                        }
                    }
                }
            }

            Dispatcher.Invoke(() => d.UpdateProgress("Merging Texture Mods..."));
            foreach (ModListItem mod in TextureMods)
            {
                ModData.TextureMod data = ModData.TextureMod.Load(File.ReadAllText(Path.Join(mod.ModDirectory, "data.json")));
                foreach (string folderPath in Directory.GetDirectories(Path.Join(mod.ModDirectory, "content")))
                {
                    string folder = Path.GetFileName(folderPath);
                    if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder)))
                        Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder));
                    foreach (string fileFolderPath in Directory.GetDirectories(folderPath))
                    {
                        string file = Path.GetFileName(fileFolderPath);
                        string igaPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file);
                        if (!File.Exists(igaPath))
                            igaPath = Path.Join(config.Paths.Loadiine, "content", folder, file);
                        IGA_File iga = new IGA_File(igaPath, IGA_Version.SkylandersTrapTeam);
                        Stream[] fileData = new Stream[iga.numberOfFiles];
                        Dictionary<string, int> pathToIdx = new Dictionary<string, int>();
                        foreach (string path in Directory.GetDirectories(fileFolderPath))
                            pathToIdx[File.ReadAllText(Path.Join(path, "path.txt"))] = int.Parse(Path.GetFileName(path));
                        for (uint i = 0; i < iga.numberOfFiles; i++)
                        {
                            MemoryStream stream = new MemoryStream();
                            iga.ExtractFile(i, stream, out _, true);
                            if (pathToIdx.ContainsKey(iga.names[i]))
                            {
                                List<uint> offsets = new List<uint>();
                                foreach (string path in Directory.GetFiles(Path.Join(fileFolderPath, pathToIdx[iga.names[i]].ToString())))
                                    if (Path.GetFileName(path) != "path.txt")
                                        offsets.Add(uint.Parse(Path.GetFileNameWithoutExtension(path)));
                                var igz = new IGZ_File(stream);
                                IGZ_RVTB rvtb = igz.fixups.First(x => x.magicNumber == 0x52565442) as IGZ_RVTB;
                                IGZ_TMET tmet = igz.fixups.First(x => x.magicNumber == 0x544D4554) as IGZ_TMET;
                                for (int j = 1; j < rvtb.count - 1; j++)
                                {
                                    if (tmet.typeNames[igz.objectList._objects[j].name].Equals("igImage2"))
                                    {
                                        if (offsets.Contains(igz.objectList._objects[j].offset))
                                        {
                                            var imageStream = new FileStream(Path.Join(fileFolderPath, pathToIdx[iga.names[i]].ToString(), $"{igz.objectList._objects[j].offset}.dds"), FileMode.Open);
                                            ((igImage2)igz.objectList._objects[j]).ReplaceDDS(imageStream);
                                        }
                                    }
                                }
                                stream.Seek(0, SeekOrigin.Begin);
                                fileData[i] = stream;
                            }
                            else
                            {
                                stream.Seek(0, SeekOrigin.Begin);
                                fileData[i] = stream;
                            }
                        }
                        iga.Build(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file), fileData);
                        iga.Close();
                    }
                }
            }

            Dispatcher.Invoke(() => d.UpdateProgress("Merging Language Mods..."));
            foreach (ModListItem mod in LanguageMods)
            {
                ModData.LanguageMod data = ModData.LanguageMod.Load(File.ReadAllText(Path.Join(mod.ModDirectory, "data.json")));
                foreach (string folderPath in Directory.GetDirectories(Path.Join(mod.ModDirectory, "content")))
                {
                    string folder = Path.GetFileName(folderPath);
                    if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder)))
                        Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder));
                    foreach (string fileFolderPath in Directory.GetDirectories(folderPath))
                    {
                        string file = Path.GetFileName(fileFolderPath);
                        string igaPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file);
                        if (!File.Exists(igaPath))
                            igaPath = Path.Join(config.Paths.Loadiine, "content", folder, file);
                        IGA_File iga = new IGA_File(igaPath, IGA_Version.SkylandersTrapTeam);
                        Stream[] fileData = new Stream[iga.numberOfFiles];
                        for (uint i = 0; i < iga.numberOfFiles; i++)
                        {
                            MemoryStream stream = new MemoryStream();
                            iga.ExtractFile(i, stream, out _, true);
                            Console.WriteLine(Path.Join(fileFolderPath, $"{iga.names[i]}.json"));
                            if (File.Exists(Path.Join(fileFolderPath, $"{iga.names[i]}.json")))
                            {
                                LanguagePak pak = new LanguagePak(stream);
                                string[] langdata = pak.unpack();
                                Dictionary<int, string> edits = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(Path.Join(fileFolderPath, $"{iga.names[i]}.json")));
                                foreach (int idx in edits.Keys)
                                {
                                    Console.WriteLine(idx + ": " + edits[idx]);
                                    langdata[idx] = edits[idx];
                                }
                                stream = new MemoryStream();
                                pak.pack(langdata, stream);
                                stream.Seek(0, SeekOrigin.Begin);
                                fileData[i] = stream;
                            }
                            stream.Seek(0, SeekOrigin.Begin);
                            fileData[i] = stream;
                        }
                        iga.Build(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", folder, file), fileData);
                        iga.Close();
                    }
                }
            }

            Dispatcher.Invoke(() => d.UpdateProgress("Merging Skylander Mods..."));
            Dictionary<string, int> modToID = new Dictionary<string, int> { };
            List<string> ssa = new List<string> { };
            List<string> sg = new List<string> { };
            List<string> ssf = new List<string> { };
            List<string> stt = new List<string> { };
            List<string> mini = new List<string> { };
            List<string> swap_top = new List<string> { };
            List<string> swap_bot = new List<string> { };
            Dictionary<string, byte[]> oldModToDump = new Dictionary<string, byte[]> { };
            if (File.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "SkylanderIDs.json")) && Directory.Exists(Path.Join(config.Paths.Dump)))
            {
                Dictionary<string, int> oldModToID = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "SkylanderIDs.json")));
                foreach (string dumppath in Directory.GetFiles(Path.Join(config.Paths.Dump), "*.*", SearchOption.AllDirectories))
                {
                    Console.WriteLine(dumppath);
                    byte[] dump = File.ReadAllBytes(dumppath);
                    int id = SkylanderDumps.GetID(dump);
                    int variantId = SkylanderDumps.GetVariantID(dump);
                    if (oldModToID.ContainsValue(id))
                    {
                        foreach (var item in oldModToID)
                        {
                            if (item.Value == id)
                            {
                                File.WriteAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Skylander Backups", $"{item.Key}-{variantId}.sky"), dump);
                                break;
                            }
                        }
                    }
                }
                foreach (string filePath in Directory.GetFiles(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Skylander Backups"), "*.sky"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    byte[] dump = File.ReadAllBytes(filePath);
                    oldModToDump[fileName] = dump;
                }
            }
            if (Directory.Exists(Path.Join(config.Paths.Dump)))
            {
                Directory.Delete(Path.Join(config.Paths.Dump), true);
            }
            Directory.CreateDirectory(Path.Join(config.Paths.Dump));
            foreach (ModListItem mod in SkylanderMods)
            {
                ModData.SkylanderMod data = ModData.SkylanderMod.Load(File.ReadAllText(Path.Join(mod.ModDirectory, "data.json")));
                string ModID = GetModID(data);
                string internalName = GenerateInternalName(modToID.Values.Count);
                modToID[ModID] = 5000 + modToID.Values.Count;
                if (data.skylander.type == "swap")
                {
                    modToID[ModID + "-bot"] = 5000 + modToID.Values.Count;
                    File.Copy(Path.Join(mod.ModDirectory, "skylander", "skylander_top.bld"), Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", $"{modToID[ModID]}_{internalName}_top.bld"));
                    File.Copy(Path.Join(mod.ModDirectory, "skylander", "skylander_top.arc"), Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", $"{modToID[ModID]}_{internalName}_top.arc"));
                    File.Copy(Path.Join(mod.ModDirectory, "skylander", "skylander_bot.bld"), Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", $"{modToID[ModID + "-bot"]}_{internalName}_bot.bld"));
                    File.Copy(Path.Join(mod.ModDirectory, "skylander", "skylander_bot.arc"), Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", $"{modToID[ModID + "-bot"]}_{internalName}_bot.arc"));
                }
                else
                {
                    File.Copy(Path.Join(mod.ModDirectory, "skylander", "skylander.bld"), Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", $"{modToID[ModID]}_{internalName}.bld"));
                    File.Copy(Path.Join(mod.ModDirectory, "skylander", "skylander.arc"), Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", $"{modToID[ModID]}_{internalName}.arc"));
                }

                //Generate Variant IDs
                int year = 0;
                if (data.skylander.game == "ssa") year = 0;
                if (data.skylander.game == "sg") year = 1;
                if (data.skylander.game == "ssf") year = 2;
                if (data.skylander.game == "stt") year = 3;
                if (data.skylander.variants.normal)
                    GenerateSkylanderDump(mod, data, ModID, modToID, oldModToDump, GenVariantID(year));
                if (data.skylander.variants.s2)
                    GenerateSkylanderDump(mod, data, ModID, modToID, oldModToDump, GenVariantID(1, true, false, false, false, 1), "Series 2");
                if (data.skylander.variants.s3)
                    GenerateSkylanderDump(mod, data, ModID, modToID, oldModToDump, GenVariantID(2, true, false, false, false, 5), "Series 3");
                if (data.skylander.variants.s4)
                    GenerateSkylanderDump(mod, data, ModID, modToID, oldModToDump, GenVariantID(3, true, false, false, false, 9), "Series 4");
                if (data.skylander.variants.lightcore)
                    GenerateSkylanderDump(mod, data, ModID, modToID, oldModToDump, GenVariantID(year, false, false, true, false, 6), data.skylander.type == "giant" ? "Giant" : "Lightcore");
                if (data.skylander.variants.eon)
                    GenerateSkylanderDump(mod, data, ModID, modToID, oldModToDump, GenVariantID(3, true, false, false, false, 16), "Eon's Elite");

                string lookupData = $"{modToID[ModID]},{data.skylander.element},{internalName},{data.skylander.name.Replace(",", "")}";
                if (data.skylander.type == "giant")
                {
                    lookupData += ",giant";
                }
                else if (data.skylander.type == "ranger")
                {
                    lookupData += ",ranger";
                }
                if (data.skylander.type == "mini")
                {
                    mini.Add($"{modToID[ModID]},{data.skylander.element},{$"{modToID[ModID]}_{internalName}"},{data.skylander.name.Replace(",", "")}");
                }
                else if (data.skylander.type == "swap")
                {
                    swap_top.Add(lookupData);
                    swap_bot.Add($"{modToID[$"{ModID}-bot"]},{data.skylander.element},{internalName},{data.skylander.name.Replace(",", "")}");
                }
                else if (data.skylander.game == "ssa")
                {
                    ssa.Add(lookupData);
                }
                else if (data.skylander.game == "sg")
                {
                    sg.Add(lookupData);
                }
                else if (data.skylander.game == "ssf")
                {
                    ssf.Add(lookupData);
                }
                else if (data.skylander.game == "stt")
                {
                    stt.Add(lookupData);
                }
            }
            File.WriteAllText(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "SkylanderIDs.json"), JsonConvert.SerializeObject(modToID, Formatting.Indented));
            BuildInitSetup(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content", "character", "Init_Setup.bld"), ssa, sg, ssf, stt, mini, swap_top, swap_bot);
            if (graphicPackDir != null) CopyDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "content"), Path.Join(graphicPackDir, "content"));
            Dispatcher.Invoke(() => d.Close());
        }
        public static void CopyDirectory(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
        private void BuildInitSetup(string output, List<string> ssa, List<string> sg, List<string> ssf, List<string> stt, List<string> mini, List<string> swap_top, List<string> swap_bot)
        {
            string initSetupPath = output;
            if (!File.Exists(initSetupPath))
                initSetupPath = Path.Join(config.Paths.Loadiine, "content", "character", "Init_Setup.bld");
            IGA_File iga = new IGA_File(initSetupPath, IGA_Version.SkylandersTrapTeam);
            MemoryStream[] fileData = new MemoryStream[iga.numberOfFiles];
            for (uint j = 0; j < iga.numberOfFiles; j++)
            {
                MemoryStream stream = new MemoryStream();
                iga.ExtractFile(j, stream, out _, true);
                if (iga.names[j].EndsWith(".pak"))
                {
                    LanguagePak pak = new LanguagePak(stream);
                    string[] data = pak.unpack();
                    data[210] += "\n" + string.Join("\n", ssa);
                    data[211] += "\n" + string.Join("\n", sg);
                    data[212] += "\n" + string.Join("\n", ssf);
                    data[213] += "\n" + string.Join("\n", stt);
                    data[214] += "\n" + string.Join("\n", mini);
                    data[236] += "\n" + string.Join("\n", swap_bot);
                    data[238] += "\n" + string.Join("\n", swap_top);
                    stream = new MemoryStream();
                    pak.pack(data, stream);
                }
                stream.Seek(0, SeekOrigin.Begin);
                fileData[j] = stream;
            }
            iga.Build(output, fileData);
            iga.Close();
        }
        public static string FilterFilename(string filename)
        {
            return new string(filename.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)).ToArray());
        }
        private string GetSelectedElement()
        {
            if (MagicElement.IsChecked == true)
                return "magic";
            if (WaterElement.IsChecked == true)
                return "water";
            if (AirElement.IsChecked == true)
                return "air";
            if (UndeadElement.IsChecked == true)
                return "death";
            if (TechElement.IsChecked == true)
                return "tech";
            if (FireElement.IsChecked == true)
                return "fire";
            if (EarthElement.IsChecked == true)
                return "earth";
            if (LifeElement.IsChecked == true)
                return "life";
            if (DarkElement.IsChecked == true)
                return "dark";
            if (LightElement.IsChecked == true)
                return "light";
            if (KaosElement.IsChecked == true)
                return "kaos";
            return "magic";
        }
        private string GetSelectedGame()
        {
            if (SSA.IsChecked == true)
                return "ssa";
            if (SG.IsChecked == true)
                return "sg";
            if (SSF.IsChecked == true)
                return "ssf";
            if (STT.IsChecked == true)
                return "stt";
            return "ssa";
        }
        private string GetSelectedType()
        {
            if (NoneType.IsChecked == true)
                return "";
            if (GiantType.IsChecked == true)
                return "giant";
            if (RangerType.IsChecked == true)
                return "ranger";
            //if (MiniType.IsChecked == true)
            //    return "mini";
            if (SwapForceType.IsChecked == true)
                return "swap";
            return "";
        }

        private void Game_Checked(object sender, RoutedEventArgs e)
        {
            switch (GetSelectedType())
            {
                case "giant":
                    if (sender != SG) NoneType.IsChecked = true;
                    break;
                case "ranger":
                    if (sender != STT) NoneType.IsChecked = true;
                    break;
                case "mini":
                    if (sender != STT) NoneType.IsChecked = true;
                    break;
            }
        }
        bool SFMenu = false;
        private void Type_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == SwapForceType)
            {
                SSF.IsChecked = true;
                if (!SFMenu)
                {
                    SFMenu = true;
                    var translate = new ThicknessAnimation
                    {
                        From = SkylanderModGrid.Margin,
                        To = new Thickness(0, 445, 0, 0),
                        Duration = new Duration(TimeSpan.FromSeconds(0.2))
                    };
                    var fade = new DoubleAnimation
                    {
                        From = SkylanderBottomGrid.Opacity,
                        To = 1.0,
                        Duration = new Duration(TimeSpan.FromSeconds(0.2))
                    };
                    SkylanderModGrid.BeginAnimation(MarginProperty, translate);
                    SkylanderBottomGrid.BeginAnimation(OpacityProperty, fade);
                    SkylanderBld.LabelContent = "Top .bld";
                    SkylanderArc.LabelContent = "Top .arc";
                    SkylanderTopNameLabel.Content = "Top Name";
                }
            }
            else
            {
                if (SFMenu)
                {
                    SFMenu = false;
                    var translate = new ThicknessAnimation
                    {
                        From = SkylanderModGrid.Margin,
                        To = new Thickness(0, 240, 0, 0),
                        Duration = new Duration(TimeSpan.FromSeconds(0.2))
                    };
                    var fade = new DoubleAnimation
                    {
                        From = SkylanderBottomGrid.Opacity,
                        To = 0.0,
                        Duration = new Duration(TimeSpan.FromSeconds(0.2))
                    };
                    SkylanderModGrid.BeginAnimation(MarginProperty, translate);
                    SkylanderBottomGrid.BeginAnimation(OpacityProperty, fade);
                    SkylanderBld.LabelContent = ".bld";
                    SkylanderArc.LabelContent = ".arc";
                    SkylanderTopNameLabel.Content = "Name";
                }
                if (sender == GiantType)
                {
                    SG.IsChecked = true;
                }
                else if (sender == RangerType /*|| sender == MiniType*/)
                {
                    STT.IsChecked = true;
                }
            }
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
            var grid = this.Content as Grid;
            if (grid != null)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    grid.Margin = new Thickness(7);
                    ModsMenuResizeOnMaximize.Margin = new Thickness(0, 40, 0, 30);
                }
                else
                {
                    grid.Margin = new Thickness(0);
                    ModsMenuResizeOnMaximize.Margin = new Thickness(0, 40, 0, 0);
                }
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private bool isChangingMenu = false;
        private void ManageModsButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenu(0);
        }
        private void CreateModButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenu(1);
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenu(2);
        }
        private void CreditsButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenu(3);
        }
        private void CEMUButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenu(4);
        }
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string LoadiinePath = LoadiineFolderSelect.GetPath();
            string CEMUPath = CEMUFolderSelect.GetPath();
            string DumpPath = DumpFolderSelect.GetPath();
            bool LoadiineOk;
            bool CEMUOk;
            bool DumpOk;

            if (LoadiinePath == "")
            {
                LoadiineOk = false;
                LoadiineFolderSelect.SetError("Required");
            }
            else if (!Directory.Exists(LoadiinePath))
            {
                LoadiineOk = false;
                LoadiineFolderSelect.SetError("Folder does not exist");
            }
            else if (!Directory.Exists(Path.Join(LoadiinePath, "content")))
            {
                LoadiineOk = false;
                LoadiineFolderSelect.SetError("\"content\" not found");
            }
            else
            {
                LoadiineOk = true;
                LoadiineFolderSelect.SetError("");
            }

            if (CEMUPath == "")
            {
                CEMUOk = true;
                CEMUFolderSelect.SetError("");
            }
            else if (!Directory.Exists(CEMUPath))
            {
                CEMUOk = false;
                CEMUFolderSelect.SetError("Folder does not exist");
            }
            //else if (!File.Exists(Path.Join(CEMUPath, "Cemu.exe")))
            //{
            //    CEMUOk = false;
            //    CEMUFolderSelect.SetError("\"Cemu.exe\" not found");
            //}
            else
            {
                CEMUOk = true;
                CEMUFolderSelect.SetError("");
            }

            if (DumpPath == "")
            {
                DumpOk = false;
                DumpFolderSelect.SetError("Required");
            }
            else if (!Directory.Exists(DumpPath))
            {
                DumpOk = false;
                DumpFolderSelect.SetError("Folder does not exist");
            }
            else
            {
                DumpOk = true;
                DumpFolderSelect.SetError("");
            }

            if (LoadiineOk && CEMUOk && DumpOk)
            {
                config.Paths.Loadiine = LoadiinePath;
                config.Paths.Cemu = CEMUPath;
                config.Paths.Dump = DumpPath;
                config.Region = new string[] { "USA", "EUR" }[GameRegion.SelectedIndex];
                if (File.Exists(Path.Join(config.Paths.Cemu, "Cemu.exe")))
                {
                    CEMUMenuButton.Visibility = Visibility.Visible;
                    UpdateAccounts();
                }
                else
                {
                    CEMUMenuButton.Visibility = Visibility.Collapsed;
                }
                config.Save(configPath);
            }
        }
        public bool Confirm(string msg)
        {
            var confirm = new Confirm(msg);
            confirm.Owner = this;
            confirm.ShowDialog();
            return confirm.Result;
        }
        public bool Alert(string msg)
        {
            var alert = new Alert(msg);
            alert.Owner = this;
            alert.ShowDialog();
            return alert.Result;
        }
        public string SelectOption(string msg, string[] options)
        {
            var selectOptions = new SelectOptions(msg, options);
            selectOptions.Owner = this;
            selectOptions.ShowDialog();
            return selectOptions.Result;
        }
        private void Reset_Click(object sender, RoutedEventArgs e)
        {

            if (Confirm("This will reset all data,\nincluding all Mods and Settings.\nAre you sure you want to proceed?"))
            {
                File.Delete(configPath);
                ModDisplayThumbnail.Source = null;
                Directory.Delete(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Installed Mods"), true);
                Setup window = new Setup();
                window.Show();
                this.Close();
            }
        }
        private void OneClickInstall_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm("This will modify your\nwindows registry.\nAre you sure you want to proceed?"))
            {
                string exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe").Replace("\\", "\\\\");
                if (File.Exists(exePath))
                {
                    string regFileContent = $"Windows Registry Editor Version 5.00\n\n[HKEY_CLASSES_ROOT\\sttumm]\n@=\"STTUMM\"\n\"URL Protocol\"=\"\"\n[HKEY_CLASSES_ROOT\\sttumm\\shell\\open\\command]\n@=\"\\\"{exePath}\\\" \\\"%1\\\"\"";
                    string regFilePath = Path.Combine(Path.GetTempPath(), "temp.reg");
                    File.WriteAllText(regFilePath, regFileContent);
                    var regeditProcessInfo = new ProcessStartInfo
                    {
                        FileName = "regedit.exe",
                        Arguments = "/s " + regFilePath,
                        Verb = "runas",
                        UseShellExecute = true
                    };
                    try
                    {
                        Process regeditProcess = Process.Start(regeditProcessInfo);
                        regeditProcess.WaitForExit();
                        using (RegistryKey key = Registry.ClassesRoot.OpenSubKey("sttumm"))
                        {
                            if (key != null && key.GetValue("URL Protocol") != null)
                            {
                                Console.WriteLine("Installed!");
                                File.Delete(regFilePath);
                                return;
                            }
                        }
                        File.Delete(regFilePath);
                        return;
                    }
                    catch
                    {
                        File.Delete(regFilePath);
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Unable to find exe");
                }
            }
        }
        private void SwitchMenu(int idx)
        {
            if (isChangingMenu || idx == ActiveMenuIdx) return;
            isChangingMenu = true;
            MenuSelectionIndicator.BeginAnimation(MarginProperty, new ThicknessAnimation
            {
                From = MenuSelectionIndicator.Margin,
                To = new Thickness(MenuSelectionIndicator.Margin.Left, 100 + idx * 50, MenuSelectionIndicator.Margin.Right, MenuSelectionIndicator.Margin.Bottom),
                Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            });
            SwitchGrid(Menus[ActiveMenuIdx], Menus[idx], idx > ActiveMenuIdx, idx < ActiveMenuIdx);
            ActiveMenuIdx = idx;
        }
        private void SwitchModCreationMenu(int idx)
        {
            if (isChangingMenu || idx == ActiveModCreationMenuIdx) return;
            isChangingMenu = true;
            SwitchGrid(ModCreationMenus[ActiveModCreationMenuIdx], ModCreationMenus[idx], true, true);
            ActiveModCreationMenuIdx = idx;
        }
        private void SwitchGrid(FrameworkElement A, FrameworkElement B, bool fadeup1, bool fadeup2)
        {
            var translate = new ThicknessAnimation
            {
                From = new Thickness(A.Margin.Left, 0, A.Margin.Right, A.Margin.Bottom),
                To = new Thickness(A.Margin.Left, fadeup1 ? -30 : 30, A.Margin.Right, A.Margin.Bottom),
                Duration = new Duration(TimeSpan.FromSeconds(0.15)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            var fade = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.2))
            };
            translate.Completed += (s, e) => {
                A.Visibility = Visibility.Collapsed;
                var translate = new ThicknessAnimation
                {
                    From = new Thickness(B.Margin.Left, fadeup2 ? -30 : 30, B.Margin.Right, B.Margin.Bottom),
                    To = new Thickness(B.Margin.Left, 0, B.Margin.Right, B.Margin.Bottom),
                    Duration = new Duration(TimeSpan.FromSeconds(0.15)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var fade = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
                };
                B.BeginAnimation(MarginProperty, translate);
                B.BeginAnimation(OpacityProperty, fade);
                B.Visibility = Visibility.Visible;
                isChangingMenu = false;
            };
            A.BeginAnimation(MarginProperty, translate);
            A.BeginAnimation(OpacityProperty, fade);
        }
        private void AlphanumericUnderscore(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[a-zA-Z0-9_]+$"))
            {
                e.Handled = true;
            }
        }

        public static int GenVariantID(int year, bool b1 = false, bool b2 = false, bool b3 = false, bool b4 = false, int deco = 0)
        {
            string y = Convert.ToString(year, 2).PadLeft(4, '0');
            string b = $"{(b1 ? 1 : 0)}{(b2 ? 1 : 0)}{(b3 ? 1 : 0)}{(b4 ? 1 : 0)}";
            string d = Convert.ToString(deco, 2).PadLeft(8, '0');
            string combinedBinary = y + b + d;
            return Convert.ToInt32(combinedBinary, 2);
        }

        private void CreateSkylander_Click(object sender, RoutedEventArgs e)
        {
            string BldPath = SkylanderBld.GetPath();
            string ArcPath = SkylanderArc.GetPath();
            string BottomBldPath = SkylanderBottomBld.GetPath();
            string BottomArcPath = SkylanderBottomArc.GetPath();
            bool BldOk;
            bool ArcOk;
            bool BottomBldOk;
            bool BottomArcOk;

            if (BldPath == "")
            {
                BldOk = false;
                SkylanderBld.SetError("Required");
            }
            else if (!File.Exists(BldPath))
            {
                BldOk = false;
                SkylanderBld.SetError("File does not exist");
            }
            else
            {
                BldOk = true;
                SkylanderBld.SetError("");
            }

            if (ArcPath == "")
            {
                ArcOk = false;
                SkylanderArc.SetError("Required");
            }
            else if (!File.Exists(ArcPath))
            {
                ArcOk = false;
                SkylanderArc.SetError("File does not exist");
            }
            else
            {
                ArcOk = true;
                SkylanderArc.SetError("");
            }

            if (BottomBldPath == "")
            {
                BottomBldOk = false;
                SkylanderBottomBld.SetError("Required");
            }
            else if (!File.Exists(BldPath))
            {
                BottomBldOk = false;
                SkylanderBottomBld.SetError("File does not exist");
            }
            else
            {
                BottomBldOk = true;
                SkylanderBottomBld.SetError("");
            }

            if (BottomArcPath == "")
            {
                BottomArcOk = false;
                SkylanderBottomArc.SetError("Required");
            }
            else if (!File.Exists(BldPath))
            {
                BottomArcOk = false;
                SkylanderBottomArc.SetError("File does not exist");
            }
            else
            {
                BottomArcOk = true;
                SkylanderBottomArc.SetError("");
            }

            if ((SwapForceType.IsChecked == false || (BottomBldOk && BottomArcOk)) && BldOk && ArcOk)
            {
                ModData.SkylanderMod data = new ModData.SkylanderMod();

                MemoryStream thumbnailStream = Thumbnail.Export();
                data.name = ModName.Text;
                data.author = ModAuthor.Text;
                data.description = ModDescription.Text;
                data.version = ModVersion.Text;

                data.skylander.name = SkylanderName.Text;
                data.skylander.name_bot = SkylanderBottomName.Text;
                data.skylander.element = GetSelectedElement();
                data.skylander.game = GetSelectedGame();
                data.skylander.type = GetSelectedType();

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
                sfd.DefaultExt = "zip";
                sfd.FileName = $"{GetModID(data, true)}.zip";

                data.skylander.variants.normal = DefaultVariant.IsChecked == true;
                data.skylander.variants.s2 = S2Variant.IsChecked == true;
                data.skylander.variants.s3 = S3Variant.IsChecked == true;
                data.skylander.variants.s4 = S4Variant.IsChecked == true;
                data.skylander.variants.lightcore = LightcoreVariant.IsChecked == true;
                data.skylander.variants.eon = EonVariant.IsChecked == true;

                if (sfd.ShowDialog() == true)
                {
                    using (var zip = new ZipArchive(new FileStream(sfd.FileName, FileMode.Create), ZipArchiveMode.Create))
                    {
                        var entry = zip.CreateEntry("data.json");
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            writer.Write(data.Serialize());
                        }

                        entry = zip.CreateEntry("thumbnail.png");
                        using (var entryStream = entry.Open())
                        {
                            thumbnailStream.CopyTo(entryStream);
                        }

                        if (SwapForceType.IsChecked == true)
                        {
                            entry = zip.CreateEntry("skylander/skylander_top.bld");
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(BldPath))
                            {
                                fileStream.CopyTo(entryStream);
                            }

                            entry = zip.CreateEntry("skylander/skylander_top.arc");
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(ArcPath))
                            {
                                fileStream.CopyTo(entryStream);
                            }

                            entry = zip.CreateEntry("skylander/skylander_bot.bld");
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(BottomBldPath))
                            {
                                fileStream.CopyTo(entryStream);
                            }

                            entry = zip.CreateEntry("skylander/skylander_bot.arc");
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(BottomArcPath))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                        else
                        {
                            entry = zip.CreateEntry("skylander/skylander.bld");
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(BldPath))
                            {
                                fileStream.CopyTo(entryStream);
                            }

                            entry = zip.CreateEntry("skylander/skylander.arc");
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(ArcPath))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }
                    Thumbnail.Reset();
                    ModName.Text = "";
                    ModAuthor.Text = "";
                    ModDescription.Text = "";
                    ModVersion.Text = "1.0";
                    ModType.SelectedIndex = 0;
                    SkylanderBld.SetPath("");
                    SkylanderArc.SetPath("");
                    SkylanderBottomBld.SetPath("");
                    SkylanderBottomArc.SetPath("");
                    SkylanderBld.SetError("");
                    SkylanderArc.SetError("");
                    SkylanderBottomBld.SetError("");
                    SkylanderBottomArc.SetError("");
                    SkylanderName.Text = "";
                    MagicElement.IsChecked = true;
                    SSA.IsChecked = true;
                    NoneType.IsChecked = true;
                    DefaultVariant.IsChecked = true;
                    S2Variant.IsChecked = false;
                    S3Variant.IsChecked = false;
                    S4Variant.IsChecked = false;
                    LightcoreVariant.IsChecked = false;
                    EonVariant.IsChecked = false;
                    SwitchModCreationMenu(0);
                }
            }
        }
        private void CancelMod_Click(object sender, RoutedEventArgs e)
        {
            Thumbnail.Reset();
            ModName.Text = "";
            ModAuthor.Text = "";
            ModDescription.Text = "";
            ModVersion.Text = "1.0";
            ModType.SelectedIndex = 0;
            SkylanderBld.SetPath("");
            SkylanderArc.SetPath("");
            SkylanderBld.SetError("");
            SkylanderArc.SetError("");
            SkylanderName.Text = "";
            MagicElement.IsChecked = true;
            SSA.IsChecked = true;
            NoneType.IsChecked = true;
            DefaultVariant.IsChecked = true;
            S2Variant.IsChecked = false;
            S3Variant.IsChecked = false;
            S4Variant.IsChecked = false;
            LightcoreVariant.IsChecked = false;
            EonVariant.IsChecked = false;
            SwitchModCreationMenu(0);
        }
        public static string GetModID(ModData.ModBase data, bool includeVersion = false)
        {
            string versionString = includeVersion ? $".v{Regex.Replace(data.version, "[^0-9.]", "").Replace(".", "_")}" : "";
            return $"{Regex.Replace(data.author, "[^a-zA-Z0-9]", "_")}.{Regex.Replace(data.name, "[^a-zA-Z0-9]", "_")}{versionString}".Replace(",", "");
        }
        private void InstallMod_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == true)
            {
                foreach (string file in ofd.FileNames)
                {
                    using (ZipArchive archive = ZipFile.OpenRead(file))
                    {
                        ZipArchiveEntry entry = archive.GetEntry("data.json");
                        if (entry != null)
                        {
                            using (StreamReader reader = new StreamReader(entry.Open()))
                            {
                                ModData.ModBase data = ModData.ModBase.Load(reader.ReadToEnd());
                                string path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Installed Mods", GetModID(data));
                                if (Directory.Exists(path))
                                {
                                    Directory.Delete(path, true);
                                }
                                Directory.CreateDirectory(path);
                                ZipFile.ExtractToDirectory(file, path);
                            }
                        }
                    }
                }
                UpdateModData();
            }
        }
        private async void MergeMods_Click(object sender, RoutedEventArgs e)
        {
            await MergeWithDialog();
        }
        private async void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            await MergeWithDialog();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Path.Join(config.Paths.Cemu, "Cemu.exe");
            if (config.Region == "USA")
                info.Arguments = "-t 000500001017C600";
            else if (config.Region == "EUR")
                info.Arguments = "-t 0005000010181F00";
            Process process = new Process();
            process.StartInfo = info;
            process.Start();
        }
        public void UpdateModData()
        {
            ModDisplay.Visibility = Visibility.Hidden;
            ModDisplayLabel.Visibility = Visibility.Visible;
            SkylanderModsList.Items.Clear();
            ContentModsList.Items.Clear();
            TextureModsList.Items.Clear();
            LanguageModsList.Items.Clear();
            ModpacksList.Items.Clear();
            SkylanderMods.Clear();
            ContentMods.Clear();
            TextureMods.Clear();
            LanguageMods.Clear();
            foreach (string modPath in Directory.GetDirectories(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Installed Mods")))
            {
                if (File.Exists(Path.Join(modPath, "data.json")))
                {
                    ModData.ModBase mod = ModData.ModBase.Load(File.ReadAllText(Path.Join(modPath, "data.json")));
                    if (mod.type == "skylander")
                    {
                        ModListItem modItem = new ModListItem(modPath);
                        SkylanderModsList.Items.Add(modItem);
                        if (modItem.Enabled) SkylanderMods.Add(modItem);
                    }
                    else if (mod.type == "content")
                    {
                        ModListItem modItem = new ModListItem(modPath);
                        ContentModsList.Items.Add(modItem);
                        if (modItem.Enabled) ContentMods.Add(modItem);
                    }
                    else if (mod.type == "texture")
                    {
                        ModListItem modItem = new ModListItem(modPath);
                        TextureModsList.Items.Add(modItem);
                        if (modItem.Enabled) TextureMods.Add(modItem);
                    }
                    else if (mod.type == "language")
                    {
                        ModListItem modItem = new ModListItem(modPath);
                        LanguageModsList.Items.Add(modItem);
                        if (modItem.Enabled) LanguageMods.Add(modItem);
                    }
                    else if (mod.type == "modpack")
                    {
                        ModpackListItem modItem = new ModpackListItem(modPath);
                        if (modItem.Enabled)
                        {
                            foreach (string modpackModPath in Directory.GetDirectories(Path.Join(modPath, "mods")))
                            {
                                if (File.Exists(Path.Join(modpackModPath, "data.json")))
                                {
                                    ModData.ModBase modpackMod = ModData.ModBase.Load(File.ReadAllText(Path.Join(modpackModPath, "data.json")));
                                    ModListItem modpackModItem = new ModListItem(modpackModPath);
                                    modpackModItem.ModpackName = modItem.ModName;
                                    if (modpackMod.type == "skylander")
                                    {
                                        modItem.ModpackMods.Add(modpackModItem);
                                        if (modpackModItem.Enabled) SkylanderMods.Add(modpackModItem);
                                    }
                                    else if (modpackMod.type == "content")
                                    {
                                        modItem.ModpackMods.Add(modpackModItem);
                                        if (modpackModItem.Enabled) ContentMods.Add(modpackModItem);
                                    }
                                    else if (modpackMod.type == "texture")
                                    {
                                        modItem.ModpackMods.Add(modpackModItem);
                                        if (modpackModItem.Enabled) TextureMods.Add(modpackModItem);
                                    }
                                    else if (modpackMod.type == "language")
                                    {
                                        modItem.ModpackMods.Add(modpackModItem);
                                        if (modpackModItem.Enabled) LanguageMods.Add(modpackModItem);
                                    }
                                }
                            }
                        }
                        ModpacksList.Items.Add(modItem);
                    }
                }
            }
        }
        private void DisableMod(object sender, RoutedEventArgs e)
        {
            ModListItem mod = (ModListItem)((CheckBox)sender).DataContext;
            string path = Path.Join(mod.ModDirectory, "disabled");
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }
            UpdateModData();
        }
        private void EnableMod(object sender, RoutedEventArgs e)
        {
            ModListItem mod = (ModListItem)((CheckBox)sender).DataContext;
            string path = Path.Join(mod.ModDirectory, "disabled");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            UpdateModData();
        }
        private ModListItem DisplayedMod;
        private string DisplayedModPath;
        private void PreviewMod(object sender, RoutedEventArgs e)
        {
            foreach (ModListItem m in SkylanderModsList.Items)
            {
                var container = (ContentPresenter)SkylanderModsList.ItemContainerGenerator.ContainerFromItem(m);
                var grid = (Grid)container.ContentTemplate.FindName("ModGrid", container);
                if (grid != null)
                {
                    grid.Background = null;
                }
            }
            foreach (ModListItem m in ContentModsList.Items)
            {
                var container = (ContentPresenter)ContentModsList.ItemContainerGenerator.ContainerFromItem(m);
                var grid = (Grid)container.ContentTemplate.FindName("ModGrid", container);
                if (grid != null)
                {
                    grid.Background = null;
                }
            }
            foreach (ModListItem m in TextureModsList.Items)
            {
                var container = (ContentPresenter)TextureModsList.ItemContainerGenerator.ContainerFromItem(m);
                var grid = (Grid)container.ContentTemplate.FindName("ModGrid", container);
                if (grid != null)
                {
                    grid.Background = null;
                }
            }
            foreach (ModListItem m in LanguageModsList.Items)
            {
                var container = (ContentPresenter)LanguageModsList.ItemContainerGenerator.ContainerFromItem(m);
                var grid = (Grid)container.ContentTemplate.FindName("ModGrid", container);
                if (grid != null)
                {
                    grid.Background = null;
                }
            }
            foreach (ModpackListItem m in ModpacksList.Items)
            {
                var container = (ContentPresenter)ModpacksList.ItemContainerGenerator.ContainerFromItem(m);
                var grid = (Grid)container.ContentTemplate.FindName("ModGrid", container);
                if (grid != null)
                {
                    grid.Background = null;
                }
                foreach (ModListItem modpackMod in m.ModpackMods)
                {
                    var modpackModcontainer = (ContentPresenter)((ItemsControl)container.ContentTemplate.FindName("ModpackModsList", container)).ItemContainerGenerator.ContainerFromItem(modpackMod);
                    var modpackModGrid = (Grid)modpackModcontainer.ContentTemplate.FindName("ModGrid", modpackModcontainer);
                    if (modpackModGrid != null)
                    {
                        modpackModGrid.Background = null;
                    }
                }
            }
            ModListItem mod = (ModListItem)((Grid)sender).DataContext;
            bool inModpack = false;
            foreach (ModpackListItem m in ModpacksList.Items)
            {
                if (m.ModpackMods.Contains(mod))
                {
                    inModpack = true;
                    break;
                }
            }
            DisplayedMod = mod;
            DisplayedModPath = mod.ModDirectory;
            ((Grid)sender).Background = new SolidColorBrush(Color.FromArgb(0x60, 0, 0xFF, 0));
            ModData.ModBase data = ModData.ModBase.Load(File.ReadAllText(Path.Join(mod.ModDirectory, "data.json")));
            ModDisplayName.Content = data.name;
            ModDisplayVersion.Content = "Version " + data.version;
            ModDisplayAuthor.Content = "Created by " + data.author;
            ModDisplayDescription.Content = data.description;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(Path.Join(mod.ModDirectory, "thumbnail.png"));
            bitmapImage.EndInit();
            ModDisplayThumbnail.Source = bitmapImage;
            ModDisplayLabel.Visibility = Visibility.Hidden;
            ModDisplay.Visibility = Visibility.Visible;
            ModDeleteButton.Visibility = inModpack ? Visibility.Hidden : Visibility.Visible;
            ModSkylanderDumpOptions.Margin = inModpack ? new Thickness(0, 0, 0, 5) : new Thickness(0, 0, 0, 40);
            ModSkylanderDumpOptions.Visibility = data.type == "skylander" ? Visibility.Visible : Visibility.Hidden;
        }
        private void CreateMod(object sender, RoutedEventArgs e)
        {
            if (ModType.SelectedIndex == 0)
            {
                ModTypeSelectError.Content = "Required";
            }
            else
            {
                ModTypeSelectError.Content = "";
                SwitchModCreationMenu(ModType.SelectedIndex);
            }
        }
        private void DeleteMod(object sender, RoutedEventArgs e)
        {
            if (Confirm("This action is permanent, and\ncannot be undone.\nAre you sure you want to proceed?"))
            {
                ModDisplayThumbnail.Source = null;
                Directory.Delete(DisplayedModPath, true);
                UpdateModData();
            }
        }
        private void ExportSkylanderDump(object sender, RoutedEventArgs e)
        {
            ModListItem mod = DisplayedMod;

            string path = config.Paths.Dump;
            ModData.ModBase data = ModData.ModBase.Load(File.ReadAllText(Path.Join(DisplayedMod.ModDirectory, "data.json")));
            if (mod.ModpackName != null)
                path = Path.Join(path, SanitizeFolderName(mod.ModpackName), SanitizeFolderName(mod.ModName));
            else
                path = Path.Join(path, SanitizeFolderName(mod.ModName));

            string filename = SelectOption("Which dump do you want to export?", Directory.GetFiles(path, "*.sky").Select(Path.GetFileName).ToArray());

            if (filename != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = filename,
                    DefaultExt = ".sky",
                    Filter = "Skylander Dumps (*.sky)|*.sky"
                };

                bool? result = saveFileDialog.ShowDialog();
                if (result == true)
                {
                    File.Copy(Path.Join(path, filename), saveFileDialog.FileName);
                }
            }
        }
        private void ReplaceSkylanderDump(object sender, RoutedEventArgs e)
        {
            if (Confirm("This action is permanent, and\ncannot be undone.\nAre you sure you want to proceed?"))
            {
                ModListItem mod = DisplayedMod;

                string path = config.Paths.Dump;
                ModData.ModBase data = ModData.ModBase.Load(File.ReadAllText(Path.Join(DisplayedMod.ModDirectory, "data.json")));
                if (mod.ModpackName != null)
                    path = Path.Join(path, SanitizeFolderName(mod.ModpackName), SanitizeFolderName(mod.ModName));
                else
                    path = Path.Join(path, SanitizeFolderName(mod.ModName));

                string filename = SelectOption("Which dump do you want to replace?", Directory.GetFiles(path, "*.sky").Select(Path.GetFileName).ToArray());

                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Skylander Dumps (*.sky)|*.sky",
                    Title = "Replace Skylander Dump"
                };

                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    byte[] target = File.ReadAllBytes(Path.Join(path, filename));
                    byte[] dump = File.ReadAllBytes(openFileDialog.FileName);
                    int id = SkylanderDumps.GetID(target);
                    int variantId = SkylanderDumps.GetVariantID(target);
                    dump = SkylanderDumps.SetID(dump, id);
                    dump = SkylanderDumps.SetVariantID(dump, variantId);
                    File.WriteAllBytes(Path.Join(path, filename), dump);
                }
                else
                {
                    Console.WriteLine("File selection canceled.");
                }
            }
        }
        private void ResetSkylanderDump(object sender, RoutedEventArgs e)
        {
            if (Confirm("This action is permanent, and\ncannot be undone.\nAre you sure you want to proceed?"))
            {
                ModListItem mod = DisplayedMod;

                string path = config.Paths.Dump;
                ModData.ModBase data = ModData.ModBase.Load(File.ReadAllText(Path.Join(DisplayedMod.ModDirectory, "data.json")));
                if (mod.ModpackName != null)
                    path = Path.Join(path, SanitizeFolderName(mod.ModpackName), SanitizeFolderName(mod.ModName));
                else
                    path = Path.Join(path, SanitizeFolderName(mod.ModName));

                string filename = SelectOption("Which dump do you want to reset?", Directory.GetFiles(path, "*.sky").Select(Path.GetFileName).ToArray());

                byte[] target = File.ReadAllBytes(Path.Join(path, filename));
                int id = SkylanderDumps.GetID(target);
                int variantId = SkylanderDumps.GetVariantID(target);
                File.WriteAllBytes(Path.Join(path, filename), SkylanderDumps.Generate(id, variantId));
            }
        }
        private void CreateContentReplacementMod(object sender, RoutedEventArgs e)
        {
            string ContentPath = ContentReplacementPath.GetPath();

            if (ContentPath == "")
            {
                ContentReplacementPath.SetError("Required");
            }
            else if (!Directory.Exists(ContentPath))
            {
                ContentReplacementPath.SetError("Folder does not exist");
            }
            else if (!Directory.Exists(Path.Join(ContentPath, "content")))
            {
                ContentReplacementPath.SetError("\"content\" not found");
            }
            else
            {
                ContentReplacementPath.SetError("");

                ModData.ContentReplacementMod data = new ModData.ContentReplacementMod();

                MemoryStream thumbnailStream = Thumbnail.Export();
                data.name = ModName.Text;
                data.author = ModAuthor.Text;
                data.description = ModDescription.Text;
                data.version = ModVersion.Text;

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
                sfd.DefaultExt = "zip";
                sfd.FileName = $"{GetModID(data, true)}.zip";

                if (sfd.ShowDialog() == true)
                {
                    using (var zip = new ZipArchive(new FileStream(sfd.FileName, FileMode.Create), ZipArchiveMode.Create))
                    {
                        var entry = zip.CreateEntry("data.json");
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            writer.Write(data.Serialize());
                        }

                        entry = zip.CreateEntry("thumbnail.png");
                        using (var entryStream = entry.Open())
                        {
                            thumbnailStream.CopyTo(entryStream);
                        }

                        foreach (string folderPath in Directory.GetDirectories(Path.Join(ContentPath, "content")))
                        {
                            string folder = Path.GetFileName(folderPath);
                            foreach (string filePath in Directory.GetFiles(folderPath))
                            {
                                string file = Path.GetFileName(filePath);
                                if (!File.Exists(Path.Join(config.Paths.Loadiine, "content", folder, file)))
                                {

                                    entry = zip.CreateEntry($"content/{folder}/{file}");
                                    using (var entryStream = entry.Open())
                                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                                    {
                                        fileStream.CopyTo(entryStream);
                                    }
                                }
                                else if (IsDiff(filePath, Path.Join(config.Paths.Loadiine, "content", folder, file)))
                                {
                                    if (file.EndsWith(".bld"))
                                    {
                                        IGA_File iga1 = new IGA_File(filePath, IGA_Version.SkylandersTrapTeam);
                                        IGA_File iga2 = new IGA_File(Path.Join(config.Paths.Loadiine, "content", folder, file), IGA_Version.SkylandersTrapTeam);
                                        MemoryStream[] fileData = new MemoryStream[iga1.numberOfFiles];
                                        for (uint j = 0; j < iga1.numberOfFiles; j++)
                                        {
                                            MemoryStream stream1 = new MemoryStream();
                                            iga1.ExtractFile(j, stream1, out _, true);
                                            if (iga2.names.Contains(iga1.names[j]))
                                            {
                                                MemoryStream stream2 = new MemoryStream();
                                                iga2.ExtractFile((uint)Array.IndexOf(iga2.names, iga1.names[j]), stream2, out _, true);
                                                if (IsDiff(stream1, stream2))
                                                {
                                                    stream1.Seek(0, SeekOrigin.Begin);
                                                    entry = zip.CreateEntry($"content/{folder}/{file}/{iga1.names[j]}");
                                                    using (var entryStream = entry.Open())
                                                    {
                                                        stream1.CopyTo(entryStream);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                stream1.Seek(0, SeekOrigin.Begin);
                                                entry = zip.CreateEntry($"content/{folder}/{file}/{iga1.names[j]}");
                                                using (var entryStream = entry.Open())
                                                {
                                                    stream1.CopyTo(entryStream);
                                                }
                                            }
                                        }
                                        iga1.Close();
                                        iga2.Close();
                                    }
                                    else if (file.EndsWith(".arc"))
                                    {
                                        IGA_File iga1 = new IGA_File(filePath, IGA_Version.SkylandersTrapTeam);
                                        IGA_File iga2 = new IGA_File(Path.Join(config.Paths.Loadiine, "content", folder, file), IGA_Version.SkylandersTrapTeam);
                                        MemoryStream[] fileData = new MemoryStream[iga1.numberOfFiles];
                                        for (uint j = 0; j < iga1.numberOfFiles; j++)
                                        {
                                            MemoryStream stream1 = new MemoryStream();
                                            iga1.ExtractFile(j, stream1, out _, true);
                                            if (iga2.names.Contains(iga1.names[j]))
                                            {
                                                MemoryStream stream2 = new MemoryStream();
                                                iga2.ExtractFile((uint)Array.IndexOf(iga2.names, iga1.names[j]), stream2, out _, true);
                                                if (IsDiff(stream1, stream2))
                                                {
                                                    stream1.Seek(0, SeekOrigin.Begin);
                                                    entry = zip.CreateEntry($"content/{folder}/{file}/{j}.bin");
                                                    using (var entryStream = entry.Open())
                                                    {
                                                        stream1.CopyTo(entryStream);
                                                    }
                                                    entry = zip.CreateEntry($"content/{folder}/{file}/{j}.path");
                                                    using (var entryStream = entry.Open())
                                                    using (var writer = new StreamWriter(entryStream))
                                                    {
                                                        writer.Write(iga1.names[j]);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                stream1.Seek(0, SeekOrigin.Begin);
                                                entry = zip.CreateEntry($"content/{folder}/{file}/{j}.bin");
                                                using (var entryStream = entry.Open())
                                                {
                                                    stream1.CopyTo(entryStream);
                                                }
                                                entry = zip.CreateEntry($"content/{folder}/{file}/{j}.path");
                                                using (var entryStream = entry.Open())
                                                using (var writer = new StreamWriter(entryStream))
                                                {
                                                    writer.Write(iga1.names[j]);
                                                }
                                            }
                                        }
                                        iga1.Close();
                                        iga2.Close();
                                    }
                                    else
                                    {
                                        entry = zip.CreateEntry($"content/{folder}/{file}");
                                        using (var entryStream = entry.Open())
                                        using (var fileStream = new FileStream(filePath, FileMode.Open))
                                        {
                                            fileStream.CopyTo(entryStream);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Thumbnail.Reset();
                    ModName.Text = "";
                    ModAuthor.Text = "";
                    ModDescription.Text = "";
                    ModVersion.Text = "1.0";
                    ModType.SelectedIndex = 0;
                    ContentReplacementPath.SetPath("");
                    ContentReplacementPath.SetError("");
                    SwitchModCreationMenu(0);
                }
            }
        }
        private void CreateTextureModFile(object sender, RoutedEventArgs e)
        {
            string ContentPath = TexturePath.GetPath();

            if (ContentPath == "")
            {
                TexturePath.SetError("Required");
            }
            else if (!Directory.Exists(ContentPath))
            {
                TexturePath.SetError("Folder does not exist");
            }
            else if (!Directory.Exists(Path.Join(ContentPath, "content")))
            {
                TexturePath.SetError("\"content\" not found");
            }
            else
            {
                TexturePath.SetError("");

                ModData.TextureMod data = new ModData.TextureMod();

                MemoryStream thumbnailStream = Thumbnail.Export();
                data.name = ModName.Text;
                data.author = ModAuthor.Text;
                data.description = ModDescription.Text;
                data.version = ModVersion.Text;

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
                sfd.DefaultExt = "zip";
                sfd.FileName = $"{GetModID(data, true)}.zip";

                if (sfd.ShowDialog() == true)
                {
                    using (var zip = new ZipArchive(new FileStream(sfd.FileName, FileMode.Create), ZipArchiveMode.Create))
                    {
                        var entry = zip.CreateEntry("data.json");
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            writer.Write(data.Serialize());
                        }

                        entry = zip.CreateEntry("thumbnail.png");
                        using (var entryStream = entry.Open())
                        {
                            thumbnailStream.CopyTo(entryStream);
                        }

                        foreach (string folderPath in Directory.GetDirectories(Path.Join(ContentPath, "content")))
                        {
                            string folder = Path.GetFileName(folderPath);
                            foreach (string filePath in Directory.GetFiles(folderPath))
                            {
                                string file = Path.GetFileName(filePath);
                                if (File.Exists(Path.Join(config.Paths.Loadiine, "content", folder, file)) && IsDiff(filePath, Path.Join(config.Paths.Loadiine, "content", folder, file)))
                                {
                                    if (file.EndsWith(".bld") || file.EndsWith(".arc"))
                                    {
                                        IGA_File iga1 = new IGA_File(filePath, IGA_Version.SkylandersTrapTeam);
                                        IGA_File iga2 = new IGA_File(Path.Join(config.Paths.Loadiine, "content", folder, file), IGA_Version.SkylandersTrapTeam);
                                        MemoryStream[] fileData = new MemoryStream[iga1.numberOfFiles];
                                        for (uint j = 0; j < iga1.numberOfFiles; j++)
                                        {
                                            if (iga1.names[j].EndsWith(".bld") || iga1.names[j].EndsWith(".igz"))
                                            {
                                                MemoryStream stream1 = new MemoryStream();
                                                iga1.ExtractFile(j, stream1, out _, true);
                                                MemoryStream stream2 = new MemoryStream();
                                                iga2.ExtractFile((uint)Array.IndexOf(iga2.names, iga1.names[j]), stream2, out _, true);
                                                var igz1 = new IGZ_File(stream1);
                                                var igz2 = new IGZ_File(stream2);
                                                bool addedPath = false;
                                                IGZ_RVTB rvtb = igz1.fixups.First(x => x.magicNumber == 0x52565442) as IGZ_RVTB;
                                                IGZ_TMET tmet = igz1.fixups.First(x => x.magicNumber == 0x544D4554) as IGZ_TMET;
                                                for (int i = 1; i < rvtb.count - 1; i++)
                                                {
                                                    if (tmet.typeNames[igz1.objectList._objects[i].name].Equals("igImage2"))
                                                    {
                                                        MemoryStream imageStream1 = new MemoryStream();
                                                        ((igImage2)igz1.objectList._objects[i]).ExtractDDS(imageStream1);
                                                        MemoryStream imageStream2 = new MemoryStream();
                                                        ((igImage2)igz2.objectList._objects[i]).ExtractDDS(imageStream2);
                                                        if (IsDiff(imageStream1, imageStream2))
                                                        {
                                                            Console.WriteLine(igz1.objectList._objects[i].offset);
                                                            if (!addedPath)
                                                            {
                                                                entry = zip.CreateEntry($"content/{folder}/{file}/{j}/path.txt");
                                                                using (var entryStream = entry.Open())
                                                                using (var writer = new StreamWriter(entryStream))
                                                                {
                                                                    writer.Write(iga1.names[j]);
                                                                }
                                                                addedPath = true;
                                                            }
                                                            entry = zip.CreateEntry($"content/{folder}/{file}/{j}/{igz1.objectList._objects[i].offset}.dds");
                                                            using (var entryStream = entry.Open())
                                                            {
                                                                imageStream1.Seek(0, SeekOrigin.Begin);
                                                                imageStream1.CopyTo(entryStream);
                                                            }
                                                        }
                                                    }
                                                }
                                                igz1.Close();
                                                igz2.Close();
                                            }
                                        }
                                        iga1.Close();
                                        iga2.Close();
                                    }
                                }
                            }
                        }
                    }
                    Thumbnail.Reset();
                    ModName.Text = "";
                    ModAuthor.Text = "";
                    ModDescription.Text = "";
                    ModVersion.Text = "1.0";
                    ModType.SelectedIndex = 0;
                    TexturePath.SetPath("");
                    TexturePath.SetError("");
                    SwitchModCreationMenu(0);
                }
            }
        }
        private void CreateLanguageModFile(object sender, RoutedEventArgs e)
        {
            string ContentPath = LanguagePath.GetPath();

            if (ContentPath == "")
            {
                LanguagePath.SetError("Required");
            }
            else if (!Directory.Exists(ContentPath))
            {
                LanguagePath.SetError("Folder does not exist");
            }
            else if (!Directory.Exists(Path.Join(ContentPath, "content")))
            {
                LanguagePath.SetError("\"content\" not found");
            }
            else
            {
                LanguagePath.SetError("");

                ModData.LanguageMod data = new ModData.LanguageMod();

                MemoryStream thumbnailStream = Thumbnail.Export();
                data.name = ModName.Text;
                data.author = ModAuthor.Text;
                data.description = ModDescription.Text;
                data.version = ModVersion.Text;

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
                sfd.DefaultExt = "zip";
                sfd.FileName = $"{GetModID(data, true)}.zip";

                if (sfd.ShowDialog() == true)
                {
                    using (var zip = new ZipArchive(new FileStream(sfd.FileName, FileMode.Create), ZipArchiveMode.Create))
                    {
                        var entry = zip.CreateEntry("data.json");
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            writer.Write(data.Serialize());
                        }

                        entry = zip.CreateEntry("thumbnail.png");
                        using (var entryStream = entry.Open())
                        {
                            thumbnailStream.CopyTo(entryStream);
                        }

                        foreach (string folderPath in Directory.GetDirectories(Path.Join(ContentPath, "content")))
                        {
                            string folder = Path.GetFileName(folderPath);
                            foreach (string filePath in Directory.GetFiles(folderPath))
                            {
                                string file = Path.GetFileName(filePath);
                                if (File.Exists(Path.Join(config.Paths.Loadiine, "content", folder, file)) && IsDiff(filePath, Path.Join(config.Paths.Loadiine, "content", folder, file)))
                                {
                                    if (file.EndsWith(".bld"))
                                    {
                                        IGA_File iga1 = new IGA_File(filePath, IGA_Version.SkylandersTrapTeam);
                                        IGA_File iga2 = new IGA_File(Path.Join(config.Paths.Loadiine, "content", folder, file), IGA_Version.SkylandersTrapTeam);
                                        for (uint i = 0; i < iga1.numberOfFiles; i++)
                                        {
                                            if (iga1.names[i].EndsWith(".pak"))
                                            {
                                                MemoryStream stream1 = new MemoryStream();
                                                iga1.ExtractFile(i, stream1, out _, true);
                                                MemoryStream stream2 = new MemoryStream();
                                                iga2.ExtractFile((uint)Array.IndexOf(iga2.names, iga1.names[i]), stream2, out _, true);
                                                Dictionary<int, string> edits = new Dictionary<int, string>();
                                                if (IsDiff(stream1, stream2))
                                                {
                                                    LanguagePak pak1 = new LanguagePak(stream1);
                                                    LanguagePak pak2 = new LanguagePak(stream2);
                                                    string[] data1 = pak1.unpack();
                                                    string[] data2 = pak2.unpack();
                                                    for (int j = 0; j < data1.Length; j++)
                                                    {
                                                        if (data1[j] != data2[j])
                                                        {
                                                            edits[j] = data1[j];
                                                        }
                                                    }
                                                    entry = zip.CreateEntry($"content/{folder}/{file}/{iga1.names[i]}.json");
                                                    using (var entryStream = entry.Open())
                                                    using (var writer = new StreamWriter(entryStream))
                                                    {
                                                        writer.Write(JsonConvert.SerializeObject(edits, Formatting.Indented));
                                                    }
                                                }
                                            }
                                        }
                                        iga1.Close();
                                        iga2.Close();
                                    }
                                }
                            }
                        }
                    }
                    Thumbnail.Reset();
                    ModName.Text = "";
                    ModAuthor.Text = "";
                    ModDescription.Text = "";
                    ModVersion.Text = "1.0";
                    ModType.SelectedIndex = 0;
                    LanguagePath.SetPath("");
                    LanguagePath.SetError("");
                    SwitchModCreationMenu(0);
                }
            }
        }
        private void AddModToModpack(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == true)
            {
                foreach (string file in ofd.FileNames)
                {
                    bool isAlreadyAdded = false;
                    foreach (ModForModpack mod in ModsInModpack.Items)
                    {
                        if (mod.ModPath == file)
                        {
                            isAlreadyAdded = true;
                            break;
                        }
                    }
                    if (isAlreadyAdded)
                    {
                        continue;
                    }
                    using (ZipArchive archive = ZipFile.OpenRead(file))
                    {
                        ZipArchiveEntry entry = archive.GetEntry("data.json");
                        if (entry != null)
                        {
                            using (StreamReader reader = new StreamReader(entry.Open()))
                            {
                                ModData.ModBase data = ModData.ModBase.Load(reader.ReadToEnd());
                                if (data.type != "modpack")
                                    ModsInModpack.Items.Add(new ModForModpack(data, file));
                            }
                        }
                    }
                }
                UpdateModData();
            }
        }
        private void RemoveModFromModpack(object sender, RoutedEventArgs e)
        {
            ModsInModpack.Items.Remove((ModForModpack)((Button)sender).DataContext);
        }
        private void CreateModpack(object sender, RoutedEventArgs e)
        {
            ModData.Modpack data = new ModData.Modpack();

            MemoryStream thumbnailStream = Thumbnail.Export();
            data.name = ModName.Text;
            data.author = ModAuthor.Text;
            data.description = ModDescription.Text;
            data.version = ModVersion.Text;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Mod File (*.stt, *.zip)|*.stt;*.zip";
            sfd.DefaultExt = "zip";
            sfd.FileName = $"{GetModID(data, true)}.zip";

            if (sfd.ShowDialog() == true)
            {
                using (var zip = new ZipArchive(new FileStream(sfd.FileName, FileMode.Create), ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("data.json");
                    using (var entryStream = entry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        writer.Write(data.Serialize());
                    }

                    entry = zip.CreateEntry("thumbnail.png");
                    using (var entryStream = entry.Open())
                    {
                        thumbnailStream.CopyTo(entryStream);
                    }

                    foreach (ModForModpack mod in ModsInModpack.Items)
                    {
                        using (var modZip = new ZipArchive(File.OpenRead(mod.ModPath), ZipArchiveMode.Read))
                        {
                            foreach (var modEntry in modZip.Entries)
                            {
                                entry = zip.CreateEntry($"mods/{GetModID(mod.ModData, true)}/{modEntry.FullName}");
                                using (var entryStream = entry.Open())
                                using (var modEntryStream = modEntry.Open())
                                {
                                    modEntryStream.CopyTo(entryStream);
                                }
                            }
                        }
                    }
                }
                Thumbnail.Reset();
                ModName.Text = "";
                ModAuthor.Text = "";
                ModDescription.Text = "";
                ModVersion.Text = "1.0";
                ModType.SelectedIndex = 0;
                ModsInModpack.Items.Clear();
                SwitchModCreationMenu(0);
            }
        }

        public List<string> accIds = new List<string>();
        public void UpdateAccounts()
        {
            XDocument doc = XDocument.Load(Path.Join(config.Paths.Cemu, "settings.xml"));
            string mlc_path = doc.Element("content").Element("mlc_path").Value;
            string actPath = Path.Join(mlc_path, "usr", "save", "system", "act");
            if (AccountSelector.ItemsSource is List<string>)
                ((List<string>)AccountSelector.ItemsSource).Clear();
            else
                AccountSelector.Items.Clear();
            List<string> accounts = new List<string>();
            foreach (string folder in Directory.GetDirectories(actPath))
            {
                if (File.Exists(Path.Join(folder, "account.dat")))
                {
                    string[] lines = File.ReadAllLines(Path.Join(folder, "account.dat"));
                    string actId = "";
                    string actName = "";
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("PersistentId="))
                        {
                            actId = line.Substring("PersistentId=".Length);
                            continue;
                        }
                        if (line.StartsWith("MiiName="))
                        {
                            actName = line.Substring("MiiName=".Length);
                            byte[] actNameBytes = new byte[actName.Length / 2];
                            for (int i = 0; i < actNameBytes.Length / 2; i++)
                            {
                                actNameBytes[i * 2] = Convert.ToByte(actName.Substring(i * 4 + 2, 2), 16);
                                actNameBytes[i * 2 + 1] = Convert.ToByte(actName.Substring(i * 4, 2), 16);
                            }
                            actName = Encoding.Unicode.GetString(actNameBytes);
                            continue;
                        }
                    }
                    if (actId != null && actName != null)
                    {
                        accounts.Add($"{actName} ({actId})");
                        accIds.Add(actId);
                    }
                }
            }
            AccountSelector.ItemsSource = accounts;
            AccountSelector.SelectedIndex = -1;
        }
        private void BackupSaveData_Click(object sender, RoutedEventArgs e)
        {
            if (AccountSelector.SelectedIndex == -1)
                AccountSelector.SelectedIndex = 0;
            if (FilterFilename(BackupSaveName.Text) == "")
            {
                Alert("No save name entered.\nPlease enter a save name.");
                return;
            }
            XDocument doc = XDocument.Load(Path.Join(config.Paths.Cemu, "settings.xml"));
            string mlc_path = doc.Element("content").Element("mlc_path").Value;
            if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups")))
                Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups"));
            string savePath;
            if (config.Region == "EUR")
                savePath = Path.Join(mlc_path, "usr", "save", "00050000", "10181F00");
            else
                savePath = Path.Join(mlc_path, "usr", "save", "00050000", "1017C600");
            if (Directory.Exists(savePath))
            {
                savePath = Path.Join(savePath, "user", accIds[AccountSelector.SelectedIndex]);
                if (Directory.Exists(savePath))
                {
                    if (!Directory.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups", accIds[AccountSelector.SelectedIndex], FilterFilename(BackupSaveName.Text))))
                        Directory.CreateDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups", accIds[AccountSelector.SelectedIndex], FilterFilename(BackupSaveName.Text)));
                    CopyDirectory(savePath, Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups", accIds[AccountSelector.SelectedIndex], FilterFilename(BackupSaveName.Text)));
                    UpdateSaveData();
                }
                else
                {
                    Alert("No save files for account found.\nTry a different account.");
                }
            }
            else
            {
                Alert("No save files for game found.\nCheck game region in settings.");
            }
        }
        public void UpdateSaveData()
        {
            SaveBackupsList.Items.Clear();
            string SaveBackupRootPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Save Backups");
            if (!Directory.Exists(SaveBackupRootPath))
            {
                Directory.CreateDirectory(SaveBackupRootPath);
                return;
            }
            foreach (string accountPath in Directory.GetDirectories(SaveBackupRootPath))
            {
                string accountId = Path.GetFileName(accountPath);
                foreach (string savePath in Directory.GetDirectories(accountPath))
                {
                    SaveBackup save = new SaveBackup()
                    {
                        SaveName = Path.GetFileName(savePath),
                        Directory = savePath,
                        AccountId = accountId
                    };
                    SaveBackupsList.Items.Add(save);
                }
            }
            UpdateAccounts();
            BackupSaveName.Text = "";
        }

        private void RestoreSave_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var save = button.DataContext as SaveBackup;
            var slot = button.Tag.ToString();
            if (Confirm($"This will override your current\nSave__{slot}.\nAre you sure you want to proceed?"))
            {
                XDocument doc = XDocument.Load(Path.Join(config.Paths.Cemu, "settings.xml"));
                string mlc_path = doc.Element("content").Element("mlc_path").Value;
                string savePath;
                if (config.Region == "EUR")
                    savePath = Path.Join(mlc_path, "usr", "save", "00050000", "10181F00");
                else
                    savePath = Path.Join(mlc_path, "usr", "save", "00050000", "1017C600");
                savePath = Path.Join(savePath, "user", save.AccountId);
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);
                if (File.Exists(Path.Join(savePath, $"Save_{slot}")))
                    File.Delete(Path.Join(savePath, $"Save_{slot}"));
                File.Copy(Path.Join(save.Directory, $"Save_{slot}"), Path.Join(savePath, $"Save_{slot}"));
            }
        }
        private void DeleteSaveBackup_Click(object sender, RoutedEventArgs e)
        {
            var save = (sender as Button).DataContext as SaveBackup;
            if (Confirm("This will PERMANENTLY delete the\nsave backup.\nAre you sure you want to proceed?"))
            {
                Directory.Delete(save.Directory, true);
                UpdateSaveData();
            }
        }
    }
}
