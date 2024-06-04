using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace STTUMM.UserControls
{
    public partial class FolderSelect : UserControl
    {
        public static readonly DependencyProperty LabelContentProperty = DependencyProperty.Register("LabelContent", typeof(string), typeof(FolderSelect), new PropertyMetadata(default(string)));
        public string LabelContent
        {
            get { return (string)GetValue(LabelContentProperty); }
            set { SetValue(LabelContentProperty, value); }
        }
        public FolderSelect()
        {
            InitializeComponent();
        }
        private void FolderSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            if (Directory.Exists(Input.Text))
            {
                dialog.InitialDirectory = Input.Text;
            }
            dialog.Multiselect = false;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                Input.Text = dialog.FolderName;
                Error.Content = "";
            }
        }
        public void SetError(string msg)
        {
            Error.Content = msg;
        }
        public void SetPath(string path)
        {
            Input.Text = path;
        }
        public string GetPath()
        {
            return Input.Text;
        }
    }
}
