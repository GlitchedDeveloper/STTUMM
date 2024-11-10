using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Controls;
using System.Reflection.PortableExecutable;
using System.IO.Pipes;

namespace STTUMM
{
    public partial class OneClickDownload : Window
    {
        public OneClickDownload(string location)
        {
            InitializeComponent();
            Message.Content = "Initiating Download...";
            Console.WriteLine("Download: " + location);
            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += DownloadProgressChanged;
                client.DownloadFileCompleted += DownloadFileCompleted;
                client.DownloadFileAsync(new Uri(location), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempMod.stt"));
            }
        }
        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Message.Content = $"Downloading... {e.ProgressPercentage}%";
            Console.WriteLine(e.ProgressPercentage.ToString());
        }
        private void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "7z.exe";
                startInfo.Arguments = $"x -odownloadTemp -y TempMod.stt";
                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Process process = Process.Start(startInfo);
                process.WaitForExit();
                ModData.ModBase data = ModData.ModBase.Load(File.ReadAllText(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "downloadTemp", "data.json")));
                string path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Installed Mods", Main.GetModID(data));
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                Directory.Move(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "downloadTemp"), path);
                using (var client = new NamedPipeClientStream(".", "STTUMM_Pipe", PipeDirection.Out))
                {
                    try
                    {
                        client.Connect(1000);
                        using (var writer = new StreamWriter(client))
                        {
                            writer.AutoFlush = true;
                            writer.WriteLine("Reload Mods");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        File.Delete(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "TempMod.stt"));
                        this.Close();
                    }
                }
            }
            else
            {
                File.Delete(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "TempMod.stt"));
                this.Close();
            }
        }
        private void Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
