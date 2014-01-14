using System;
using System.Collections.Generic;
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
using Microsoft.Kinect;

namespace KinnectExample
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor miKinect;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindowLoaded;
        }

        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Comprobamos que tenemos un sensor conectado
            if (KinectSensor.KinectSensors.Count > 0)
            {
                //Evento ejecutado al cerrar
                Closing += MainClosing;
                // Escogemos el primer sensor kinect que tengamos conectado. Puede haber más de un kinect conectado
                miKinect = KinectSensor.KinectSensors[0];
                // Habilitamos la cámara elegiendo el formato de imagen.
                miKinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                // Arrancamos Kinect.
                miKinect.Start();
                // Nos suscribimos al método
                miKinect.AllFramesReady += KinectAllFramesReady;
            }

        }

        /// <summary>
        /// Obtenemos los frames y los pintamos en la imagen.
        /// </summary>
        /// <param name="sender">
        /// <param name="e">

        void KinectAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            //Obtenemos el frame de imagen de la camara
            using (var colorFrame = e.OpenColorImageFrame())
            {
                // Si este es null no continuamos
                if (colorFrame == null) return;
                // Creamos un array de bytes del tamaño de los pixel del frame.
                byte[] pixels = new byte[colorFrame.PixelDataLength];
                //Copiamos los pixel del frame a nuestro array de bytes.
                colorFrame.CopyPixelDataTo(pixels);
                // Colocamos los pixel del frame en la imagen del xml
                int stride = colorFrame.Width * 4;
                imageKinect.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
                //imageKinect es el objeto imagen colocado en el xaml
            }
        }

        void MainClosing(object sender, EventArgs e)
        {
            // Al cerrar paramos Kinect
            if (miKinect != null)
            {
                miKinect.Stop();
            }
        }
    }
}
