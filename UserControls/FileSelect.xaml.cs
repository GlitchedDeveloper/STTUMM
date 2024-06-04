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
    public partial class FileSelect : UserControl
    {
        public static readonly DependencyProperty LabelContentProperty = DependencyProperty.Register("LabelContent", typeof(string), typeof(FileSelect), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register("Filter", typeof(string), typeof(FileSelect), new PropertyMetadata("All files (*.*)|*.*"));
        public string LabelContent
        {
            get { return (string)GetValue(LabelContentProperty); }
            set { SetValue(LabelContentProperty, value); }
        }
        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        public FileSelect()
        {
            InitializeComponent();
        }
        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = this.Filter;
            if (Directory.Exists(Input.Text))
            {
                dialog.InitialDirectory = Input.Text;
            }
            dialog.Multiselect = false;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                Input.Text = dialog.FileName;
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
