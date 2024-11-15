﻿using System.Windows;
using System.Windows.Input;

namespace STTUMM
{
    public partial class Alert : Window
    {
        public bool Result = false;

        public Alert(string message)
        {
            InitializeComponent();
            Message.Content = message;
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
        private void Yes(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.Close();
        }
    }
}
