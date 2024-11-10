using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace STTUMM
{
    internal class SaveBackup
    {
        public string SaveName { get; set; }
        public string Directory { get; set; }
        public string AccountId { get; set; }
        public void RestoreSaveHeader_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Test");
        }
    }
}
