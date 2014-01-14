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
using System.IO;
using WindowsInput;

namespace EjemploMovimientoSencilloMano
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    /// 
   
    public partial class MainWindow : Window
    {
        KinectSensor sensor;

        DrawingGroup drawingGroup;

        DrawingImage imageSource;

        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Bitmap that will hold opacity mask information
        /// </summary>
        private WriteableBitmap playerOpacityMaskImage = null;

        /// <summary>
        /// Intermediate storage for the depth data received from the sensor
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Intermediate storage for the player opacity mask
        /// </summary>
        private int[] playerPixelData;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private ColorImagePoint[] colorCoordinates;

        /// <summary>
        /// Inverse scaling factor between color and depth
        /// </summary>
        private int colorToDepthDivisor;

        /// <summary>
        /// Width of the depth image
        /// </summary>
        private int depthWidth;

        /// <summary>
        /// Height of the depth image
        /// </summary>
        private int depthHeight;

        /// <summary>
        /// Indicates opaque in an opacity mask
        /// </summary>
        private int opaquePixelValue = -1;

        SolidColorBrush brushred = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
        SolidColorBrush brushPelota = new SolidColorBrush(Color.FromArgb(100, 255, 125, 240));

        public MainWindow()
        {
            
            InitializeComponent();
        }

        private void windowLoad(object sender, RoutedEventArgs e)
        {

            this.drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            mano.Source = this.imageSource;

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {

                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                this.sensor.DepthStream.Enable(DepthFormat);

                this.depthWidth = this.sensor.DepthStream.FrameWidth;

                this.depthHeight = this.sensor.DepthStream.FrameHeight;

                this.sensor.ColorStream.Enable(ColorFormat);

                int colorWidth = this.sensor.ColorStream.FrameWidth;
                int colorHeight = this.sensor.ColorStream.FrameHeight;

                this.colorToDepthDivisor = colorWidth / this.depthWidth;

                // Allocate space to put the depth pixels we'll receive
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                // Allocate space to put the color pixels we'll create
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.playerPixelData = new int[this.sensor.DepthStream.FramePixelDataLength];

                this.colorCoordinates = new ColorImagePoint[this.sensor.DepthStream.FramePixelDataLength];


                this.colorBitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                // This is the bitmap we'll display on-screen
                FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();

                // BitmapSource objects like FormatConvertedBitmap can only have their properties
                // changed within a BeginInit/EndInit block.
                newFormatedBitmapSource.BeginInit();

                // Use the BitmapSource object defined above as the source for this new 
                // BitmapSource (chain the BitmapSource objects together).
                newFormatedBitmapSource.Source = colorBitmap;

                // Because the DestinationFormat for the FormatConvertedBitmap will be an
                // indexed pixel format (Indexed1),a DestinationPalette also needs to be specified.
                // Below, create a custom two color palette to be used for the DestinationPalette.
                System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();
                colors.Add(Colors.Purple);
                BitmapPalette myPalette = new BitmapPalette(colors);

                // Set the DestinationPalette property to the custom palette created above.
                newFormatedBitmapSource.DestinationPalette = myPalette;

                // Set the DestinationFormat to the palletized pixel format of Indexed1.
                newFormatedBitmapSource.DestinationFormat = PixelFormats.Indexed1;
                newFormatedBitmapSource.EndInit();
                // Set the image we display to point to the bitmap where we'll put the image data
                this.fondo.Source = newFormatedBitmapSource;

                // Add an event handler to be called whenever there is new depth frame data
                this.sensor.AllFramesReady += this.SensorAllFramesReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                    
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, 640, 480));
                dc.DrawRectangle(brushred, null, new Rect(200.0, 200.0, 80, 80));
                dc.DrawRectangle(brushred, null, new Rect(300.0, 100.0, 80, 80));
                dc.DrawRectangle(brushred, null, new Rect(400.0, 200.0, 80, 80));
                dc.DrawRectangle(brushred, null, new Rect(300.0, 300.0, 80, 80));

                if (skeletons.Length != 0)
                {
                    foreach(Skeleton skel in skeletons){
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.moverMano(skel, dc);
                            this.ponergorrito(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawRectangle(Brushes.YellowGreen, null, new Rect(0.0, 0.0, 640, 480));
                        }
                    }
                }
            }
        }

        private void moverMano(Skeleton skel, DrawingContext dc)
        {
            Point punto = this.SkeletonPointToScreen(skel.Joints[JointType.HandLeft].Position);
            mostrar(skel.Joints[JointType.HandLeft].Position);
            Point punto2 = this.SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position);
            Double manoDerechaZ = skel.Joints[JointType.HandRight].Position.Z;
            Double manoIzquierdaZ = skel.Joints[JointType.HandLeft].Position.Z;

            /*if ((punto.X >= 100 && punto.Y >= 100 && punto.X <= 240 && punto.Y <= 280 && manoIzquierdaZ <= 1.5 && manoIzquierdaZ >= 1) ||
                (punto2.X >= 200 && punto2.Y >= 200 && punto2.X <= 280 && punto2.Y <= 280 && manoDerechaZ <= 1.5 && manoDerechaZ >= 1))
            {
                dc.DrawRectangle(brush, null, new Rect(200.0, 200.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD3);
            }*/
            if ((punto.Y >= 150 && punto.X <= 200 && punto.Y <= 320/* && manoIzquierdaZ <= 1.5 && manoIzquierdaZ >= 1*/) ||
                (punto2.X >= 200 && punto2.Y >= 200 && punto2.X <= 280 && punto2.Y <= 280 && manoDerechaZ <= 1.5 && manoDerechaZ >= 1))
            {
                dc.DrawRectangle(brush, null, new Rect(200.0, 200.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD3);
            }
            /*if ((punto.X >= 300 && punto.Y >= 200 && punto.X <= 380 && punto.Y <= 280 && manoIzquierdaZ <= 1 && manoIzquierdaZ >= 0.5) ||
                (punto2.X >= 300 && punto2.Y >= 200 && punto2.X <= 380 && punto2.Y <= 280 && manoDerechaZ <= 1 && manoDerechaZ >= 0.5))
            {
                dc.DrawRectangle(brush, null, new Rect(300.0, 100.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD1);
            }*/
            if ((punto.X >= 280 && punto.X <= 400 && punto.Y <= 180/* && manoIzquierdaZ <= 1 && manoIzquierdaZ >= 0.5*/) ||
                (punto2.X >= 280 && punto2.X <= 400 && punto2.Y <= 180/* && manoDerechaZ <= 1 && manoDerechaZ >= 0.5*/))
            {
                dc.DrawRectangle(brush, null, new Rect(300.0, 100.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD1);
            }
            /*if ((punto.X >= 400 && punto.Y >= 200 && punto.X <= 480 && punto.Y <= 280 && manoIzquierdaZ <= 1.5 && manoIzquierdaZ >= 1) ||
                (punto2.X >= 500 && punto2.Y >= 200 && punto2.X <= 680 && punto2.Y <= 280/* && manoDerechaZ <= 1.5 && manoDerechaZ >= 1))
            {
                dc.DrawRectangle(brush, null, new Rect(400.0, 200.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD2);
            }*/
            if ((punto.X >= 400 && punto.Y >= 200 && punto.Y <= 320 && manoIzquierdaZ <= 1.5 && manoIzquierdaZ >= 1) ||
                (punto2.X >= 500 && punto2.Y >= 200 && punto2.X <= 680 && punto2.Y <= 350/* && manoDerechaZ <= 1.5 && manoDerechaZ >= 1*/))
            {
                dc.DrawRectangle(brush, null, new Rect(400.0, 200.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD2);
            }
            /*if ((punto.X >= 300 && punto.Y >= 200 && punto.X <= 380 && punto.Y <= 280 && manoIzquierdaZ <= 2 && manoIzquierdaZ >= 1.5) ||
                (punto2.X >= 300 && punto2.Y >= 200 && punto2.X <= 380 && punto2.Y <= 280 && manoDerechaZ <= 2 && manoDerechaZ >= 1.5))
            {
                dc.DrawRectangle(brush, null, new Rect(300.0, 300.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD4);
            }*/
            if ((punto.X >= 280 && punto.Y >= 300 && punto.X <= 400/* && manoIzquierdaZ <= 2 && manoIzquierdaZ >= 1.5*/) ||
                (punto2.X >= 280 && punto2.Y >= 300 && punto2.X <= 400/* && manoDerechaZ <= 2 && manoDerechaZ >= 1.5*/))
            {
                dc.DrawRectangle(brush, null, new Rect(300.0, 300.0, 80, 80));
                InputSimulator.SimulateKeyPress(VirtualKeyCode.NUMPAD4);
            }
            //ejemplo de profundidad(tienes que estar a una profundidad especifica, ha sido el primer ejemplo del eje Z)
            /*if (manoDerechaZ >= 1.5 || manoIzquierdaZ >=1.5)
            {
                dc.DrawRectangle(brush, null, new Rect(300.0, 300.0, 80, 80));
            }
            if (manoIzquierdaZ <= 1 || manoDerechaZ <=1)
            {
                dc.DrawRectangle(brush, null, new Rect(300.0, 100.0, 80, 80));
            }*/
            if (manoIzquierdaZ == 0)
            {
                manoIzquierdaZ = 1;
            } 
            if (manoDerechaZ == 0)
            {
                manoDerechaZ = 1;
            }
            double radioIzquierda = 10 / manoIzquierdaZ * 5;
            double radioDerecha = 10 / manoDerechaZ * 5;
            Point puntoExactoIzquierda = new Point(punto.X, punto.Y + radioIzquierda / 2);
            Point puntoExactoDerecha = new Point(punto2.X, punto2.Y + radioDerecha / 2);

            dc.DrawEllipse(brushPelota, null, puntoExactoIzquierda, radioIzquierda, radioIzquierda);
            dc.DrawEllipse(brushPelota, null, puntoExactoDerecha, radioDerecha, radioDerecha);
        }

        private void ponergorrito(Skeleton skel, DrawingContext dc)
        {
            Point cabeza = SkeletonPointToScreen(skel.Joints[JointType.Head].Position);
            sombrero.Margin = new Thickness(cabeza.X-100, cabeza.Y-130, 500 - cabeza.X, 340 - cabeza.Y);
            //dc.DrawRectangle(Brushes.Aqua, null, new Rect(cabeza.X, cabeza.Y, 100, 100));
        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);

            return new Point(depthPoint.X, depthPoint.Y);
        }

        private Point mostrar(SkeletonPoint skelpoint)
        {
            System.Console.Out.WriteLine(skelpoint.Y);
            return new Point(skelpoint.X, skelpoint.Y);
        }

        //lo que va a hacer esta funcion es recortar el array de pixeles que recibe la camara, con 
        //el que esta recogiendo el tracking del jugador para mostrar los pixeles que le pertenecen a estos
        //y asi desacplarlos de la imagen.
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, so nothing to do
            if (null == this.sensor)
            {
                return;
            }

            bool depthReceived = false;
            bool colorReceived = false;

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (null != depthFrame)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    depthReceived = true;
                }
            }

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (null != colorFrame)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    colorReceived = true;
                }
            }

            // do our processing outside of the using block
            // so that we return resources to the kinect as soon as possible
            if (true == depthReceived)
            {
                this.sensor.CoordinateMapper.MapDepthFrameToColorFrame(
                    DepthFormat,
                    this.depthPixels,
                    ColorFormat,
                    this.colorCoordinates);

                Array.Clear(this.playerPixelData, 0, this.playerPixelData.Length);

                // loop over each row and column of the depth
                for (int y = 0; y < this.depthHeight; ++y)
                {
                    for (int x = 0; x < this.depthWidth; ++x)
                    {
                        // calculate index into depth array
                        int depthIndex = x + (y * this.depthWidth);

                        DepthImagePixel depthPixel = this.depthPixels[depthIndex];
                        int player = depthPixel.PlayerIndex;

                        // if we're tracking a player for the current pixel, sets it opacity to full
                        if (player > 0)
                        {
                            // retrieve the depth to color mapping for the current depth pixel
                            ColorImagePoint colorImagePoint = this.colorCoordinates[depthIndex];

                            // scale color coordinates to depth resolution
                            int colorInDepthX = colorImagePoint.X / this.colorToDepthDivisor;
                            int colorInDepthY = colorImagePoint.Y / this.colorToDepthDivisor;

                            // make sure the depth pixel maps to a valid point in color space
                            // check y > 0 and y < depthHeight to make sure we don't write outside of the array
                            // check x > 0 instead of >= 0 since to fill gaps we set opaque current pixel plus the one to the left
                            // because of how the sensor works it is more correct to do it this way than to set to the right
                            if (colorInDepthX > 0 && colorInDepthX < this.depthWidth && colorInDepthY >= 0 && colorInDepthY < this.depthHeight)
                            {
                                // calculate index into the player mask pixel array
                                int playerPixelIndex = colorInDepthX + (colorInDepthY * this.depthWidth);

                                // set opaque
                                this.playerPixelData[playerPixelIndex] = opaquePixelValue;

                                // compensate for depth/color not corresponding exactly by setting the pixel 
                                // to the left to opaque as well
                                this.playerPixelData[playerPixelIndex - 1] = opaquePixelValue;
                            }
                        }
                    }
                }
            }

            // do our processing outside of the using block
            // so that we return resources to the kinect as soon as possible
            if (true == colorReceived)
            {
                // Write the pixel data into our bitmap
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels,
                    this.colorBitmap.PixelWidth * sizeof(int),
                    0);

                if (this.playerOpacityMaskImage == null)
                {
                    this.playerOpacityMaskImage = new WriteableBitmap(
                        this.depthWidth,
                        this.depthHeight,
                        96,
                        96,
                        PixelFormats.Bgra32,
                        null);
                    fondo.OpacityMask = new ImageBrush { ImageSource = this.playerOpacityMaskImage };
                }
                this.playerOpacityMaskImage.WritePixels(
                    new Int32Rect(0, 0, this.depthWidth, this.depthHeight),
                    this.playerPixelData,
                    this.depthWidth * ((this.playerOpacityMaskImage.Format.BitsPerPixel + 7) / 8),
                    0);
            }
        }

    private void windowClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }
    }   
}
