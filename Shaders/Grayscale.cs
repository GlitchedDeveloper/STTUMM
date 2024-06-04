using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows;

namespace STTUMM.Shaders
{
    class Grayscale : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(Grayscale), 0);
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(Grayscale), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Grayscale()
        {
            PixelShader pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("/assets/shaders/grayscale.ps", UriKind.Relative);
            this.PixelShader = pixelShader;

            this.UpdateShaderValue(InputProperty);
            this.UpdateShaderValue(ValueProperty);
        }
    }
}