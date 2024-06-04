using System.Windows;
using System.Windows.Input;

namespace STTUMM
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog(string message)
        {
            InitializeComponent();
            Message.Content = message;
        }
        public void UpdateProgress(string message)
        {
            Message.Content = message;
        }
        private void Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
