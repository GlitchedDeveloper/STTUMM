using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace STTUMM.UserControls
{
    public partial class ImageSelect : UserControl
    {
        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(int), typeof(ImageSelect), new PropertyMetadata(200));
        public int ImageWidth
        {
            get { return (int)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }

        public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof(int), typeof(ImageSelect), new PropertyMetadata(200));
        public int ImageHeight
        {
            get { return (int)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }
        public static readonly DependencyProperty LabelContentProperty = DependencyProperty.Register("LabelContent", typeof(string), typeof(ImageSelect), new PropertyMetadata(default(string)));
        public string LabelContent
        {
            get { return (string)GetValue(LabelContentProperty); }
            set { SetValue(LabelContentProperty, value); }
        }
        public string src;
        public ImageSelect()
        {
            InitializeComponent();
        }
        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";
            if (Directory.Exists(src))
            {
                dialog.InitialDirectory = src;
            }
            dialog.Multiselect = false;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                src = dialog.FileName;
                Error.Content = "";
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(src);
                bitmap.DecodePixelWidth = ImageWidth;
                bitmap.DecodePixelHeight = ImageHeight;
                bitmap.EndInit();
                ImageDisplay.Source = bitmap;
                Error.Content = "";
            }
        }
        public void SetError(string msg)
        {
            Error.Content = msg;
        }
        public void SetPath(string path)
        {
            src = path;
            Error.Content = "";
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(src);
            bitmap.DecodePixelWidth = ImageWidth;
            bitmap.DecodePixelHeight = ImageHeight;
            bitmap.EndInit();
            ImageDisplay.Source = bitmap;
        }
        public string GetPath()
        {
            return src;
        }
        public MemoryStream Export()
        {
            BitmapImage bitmapImage = ImageDisplay.Source as BitmapImage;
            if (bitmapImage == null)
            {
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(ImageDisplay.Source.ToString(), UriKind.RelativeOrAbsolute);
                bitmapImage.EndInit();
            }
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            MemoryStream memoryStream = new MemoryStream();
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public void Reset()
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri("pack://application:,,,/STTUMM;component/assets/placeholder.png");
            bitmapImage.EndInit();
            ImageDisplay.Source = bitmapImage;
        }
    }
}
