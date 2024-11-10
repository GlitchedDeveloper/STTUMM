using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace STTUMM
{
    public partial class SelectOptions : Window
    {
        public string Result = null;

        private object LastSelected;
        public SelectOptions(string message, string[] options)
        {
            InitializeComponent();
            Message.Content = message;

            OptionsItemsControl.ItemsSource = options;
        }

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(0x60, 0x0, 0xff, 0x0));
            }
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
            }
        }
        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.DataContext == LastSelected)
                {
                    Result = (string)border.DataContext;
                    this.Close();
                }
                else
                {
                    LastSelected = border.DataContext;
                }
            }
        }
        private void Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
