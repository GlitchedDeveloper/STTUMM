using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace STTUMM
{
    public partial class Setup : Window
    {
        private Config config;
        private string configPath;
        public Setup()
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            config = Config.Load(configPath);
            string LoadiinePath = config.Paths.Loadiine;
            string CEMUPath = config.Paths.Cemu;
            string DumpPath = config.Paths.Dump;
            if (CheckPaths(LoadiinePath, CEMUPath, DumpPath, false))
            {
                OpenMainWindow();
            }
            else
            {
                InitializeComponent();
                LoadiineFolderSelect.SetPath(config.Paths.Loadiine);
                CEMUFolderSelect.SetPath(config.Paths.Cemu);
                DumpFolderSelect.SetPath(config.Paths.Dump);
                CheckPaths(LoadiinePath, CEMUPath, DumpPath);
            }
        }

        private void OpenMainWindow()
        {
            Main window = new Main(config);
            window.Show();
            this.Close();
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool CheckPaths(string LoadiinePath, string CEMUPath, string DumpPath, bool showErrors = true)
        {
            bool LoadiineOk;
            bool CEMUOk;
            bool DumpOk;

            if (LoadiinePath == "")
            {
                LoadiineOk = false;
                if (showErrors) LoadiineFolderSelect.SetError("Required");
            }
            else if (!Directory.Exists(LoadiinePath))
            {
                LoadiineOk = false;
                if (showErrors) LoadiineFolderSelect.SetError("Folder does not exist");
            }
            else if (!Directory.Exists(Path.Combine(LoadiinePath, "content")))
            {
                LoadiineOk = false;
                if (showErrors) LoadiineFolderSelect.SetError("\"content\" not found");
            }
            else
            {
                LoadiineOk = true;
                if (showErrors) LoadiineFolderSelect.SetError("");
            }

            if (CEMUPath == "")
            {
                CEMUOk = true;
                if (showErrors) CEMUFolderSelect.SetError("");
            }
            else if (!Directory.Exists(CEMUPath))
            {
                CEMUOk = false;
                if (showErrors) CEMUFolderSelect.SetError("Folder does not exist");
            }
            else if (!File.Exists(Path.Combine(CEMUPath, "Cemu.exe")))
            {
                CEMUOk = false;
                if (showErrors) CEMUFolderSelect.SetError("\"Cemu.exe\" not found");
            }
            else
            {
                CEMUOk = true;
                if (showErrors) CEMUFolderSelect.SetError("");
            }

            if (DumpPath == "")
            {
                DumpOk = false;
                if (showErrors) DumpFolderSelect.SetError("Required");
            }
            else if (!Directory.Exists(DumpPath))
            {
                DumpOk = false;
                if (showErrors) DumpFolderSelect.SetError("Folder does not exist");
            }
            else
            {
                DumpOk = true;
                if (showErrors) DumpFolderSelect.SetError("");
            }
            return LoadiineOk && CEMUOk && DumpOk;
        }

        private void FinishSetupButton_Click(object sender, RoutedEventArgs e)
        {
            string LoadiinePath = LoadiineFolderSelect.GetPath();
            string CEMUPath = CEMUFolderSelect.GetPath();
            string DumpPath = DumpFolderSelect.GetPath();
            if (CheckPaths(LoadiinePath, CEMUPath, DumpPath))
            {
                config.Paths.Loadiine = LoadiinePath;
                config.Paths.Cemu = CEMUPath;
                config.Paths.Dump = DumpPath;
                config.Save(configPath);
                OpenMainWindow();
            }
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenu(SetupMenu, SetupSettings);
        }
        private double GetLowestPoint(Grid grid)
        {
            double lowestPoint = 0;
            foreach (UIElement child in grid.Children)
            {
                if (child is FrameworkElement fe)
                {
                    double childLowestPoint = 0;
                    switch (fe.VerticalAlignment)
                    {
                        case VerticalAlignment.Top:
                            childLowestPoint = fe.Margin.Top + fe.ActualHeight + fe.Margin.Bottom;
                            break;
                        case VerticalAlignment.Center:
                            childLowestPoint = (grid.ActualHeight - fe.ActualHeight) / 2 + fe.ActualHeight + fe.Margin.Top + fe.Margin.Bottom;
                            break;
                        case VerticalAlignment.Bottom:
                            childLowestPoint = grid.ActualHeight + fe.Margin.Top + fe.Margin.Bottom;
                            break;
                        case VerticalAlignment.Stretch:
                            childLowestPoint = grid.ActualHeight + fe.Margin.Top + fe.Margin.Bottom;
                            break;
                    }
                    if (childLowestPoint > lowestPoint)
                    {
                        lowestPoint = childLowestPoint;
                    }
                }
            }
            return lowestPoint;
        }
        private void SwitchMenu(FrameworkElement A, FrameworkElement B)
        {
            var translate = new ThicknessAnimation
            {
                From = new Thickness(A.Margin.Left, 0, A.Margin.Right, A.Margin.Bottom),
                To = new Thickness(A.Margin.Left, -30, A.Margin.Right, A.Margin.Bottom),
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
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
                    From = new Thickness(B.Margin.Left, -30, B.Margin.Right, B.Margin.Bottom),
                    To = new Thickness(B.Margin.Left, 0, B.Margin.Right, B.Margin.Bottom),
                    Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
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
            };
            A.BeginAnimation(MarginProperty, translate);
            A.BeginAnimation(OpacityProperty, fade);
        }
    }
}