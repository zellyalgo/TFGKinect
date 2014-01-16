﻿using System;
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
        /// Formato para la resolucion de la camara "ColorStream"
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap que contiene los bitsde la camara
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Bitmap que contendra los bits de la DephtImage (la imagen del usuario)
        /// </summary>
        private WriteableBitmap playerOpacityMaskImage = null;

        /// <summary>
        /// Intermedio donde se añadiran los pixeles para eliminarlos(los que sobren que no sean del usuario)
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermedio donde se pondran los pixeles de la camara para manejarlos sin estropear la imagen.
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Intermedio donde se pondran los pixeles del usuario
        /// </summary>
        private int[] playerPixelData;

        /// <summary>
        /// Intermedio que definira la tonalidad de los pixeles.
        /// </summary>
        private ColorImagePoint[] colorCoordinates;

        /// <summary>
        /// servira para escalar el tamaño de los pixeles.
        /// </summary>
        private int colorToDepthDivisor;

        /// <summary>
        /// ancho de la imagen
        /// </summary>
        private int depthWidth;

        /// <summary>
        /// alto de la imagen
        /// </summary>
        private int depthHeight;

        /// <summary>
        /// indicador para poner los pixeles a transparente.
        /// </summary>
        private int opaquePixelValue = -1;

        //son los pinceles que tienen los colores para los cuadrados rojo, verde y morado transparente respectivamente.
        SolidColorBrush brushred = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
        SolidColorBrush brushPelota = new SolidColorBrush(Color.FromArgb(100, 255, 125, 240));

        //booleanos que indicaran si se esta pulsando o no para no repetir interminablemente las pulsaciones.
        bool pulsadoDerecha = false;
        bool pulsadoIzquierda = false;
        bool pulsadoArriba = false;
        bool pulsadoAbajo = false;

        //distancia calculada entre el hombro izquierdo y el "hombro central"(base del cuello), para determinar la distancia de accion
        float distanciaX;
        //distancia del eje Z utilizada para poder tener un margen en el cual recalcular la distanciaX.
        float ejeZDistacia;

        public MainWindow()
        {
            
            InitializeComponent();
        }

        private void windowLoad(object sender, RoutedEventArgs e)
        {

            this.drawingGroup = new DrawingGroup();
            // Crea un imageSource con el que poder pintar dentro de el
            this.imageSource = new DrawingImage(this.drawingGroup);

            // introducimos el valor de este en un elemento de la interfaz
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

                // habilitamos el traking del skeleto.
                this.sensor.SkeletonStream.Enable();

                // añado el evento del traking del skeleto, como he puesto que la resolucion es de 30FPS, saltara 30 veces por segundo.
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                this.sensor.DepthStream.Enable(DepthFormat);

                this.depthWidth = this.sensor.DepthStream.FrameWidth;

                this.depthHeight = this.sensor.DepthStream.FrameHeight;

                this.sensor.ColorStream.Enable(ColorFormat);

                int colorWidth = this.sensor.ColorStream.FrameWidth;
                int colorHeight = this.sensor.ColorStream.FrameHeight;

                this.colorToDepthDivisor = colorWidth / this.depthWidth;

                // los pixeles que recibimos los ponemos en el array.
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                // hacemos lo mismo con la camara normal.
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.playerPixelData = new int[this.sensor.DepthStream.FramePixelDataLength];

                this.colorCoordinates = new ColorImagePoint[this.sensor.DepthStream.FramePixelDataLength];


                this.colorBitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                // este es el bitmap que mostraremos al usuario
                FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();

                // Un bitmapSource solo puede modificar sus atriburos dentro de una transaccion
                // BeginInit/EndInit.
                newFormatedBitmapSource.BeginInit();

                // añadimos el Bitmap que queremos que se muestre
                newFormatedBitmapSource.Source = colorBitmap;

                // añadimos un color para la paleat que tendra el bitmap en cuestion, esto tambien depende
                // directamente del formato de los pixeles, el indexed1 contrendra un color, vamos a usar el purpura.
                System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();
                colors.Add(Colors.Purple);
                BitmapPalette myPalette = new BitmapPalette(colors);

                // introducimos la paleta en el BitmapSource
                newFormatedBitmapSource.DestinationPalette = myPalette;

                //le ponemos el formato y acabamos la transaccion, haciendo que todos los cambios se actualicen.
                newFormatedBitmapSource.DestinationFormat = PixelFormats.Indexed1;
                newFormatedBitmapSource.EndInit();
                // ahora introducimos la imagen formateada con nuestra paleta en el elemento.
                this.fondo.Source = newFormatedBitmapSource;

                // añado el envento de la camara que mostrara los pixeles.
                this.sensor.AllFramesReady += this.SensorAllFramesReady;

                // arranco el sensor
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
                // dibujamos un rectangulo negro para el fondo.
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, 640, 480));
                
                if (skeletons.Length != 0)
                {
                    //recorremos todos los skeletos que tiene
                    foreach(Skeleton skel in skeletons){
                        //seleccionamos los que estan siendo trakeados.
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            reescalar(skel);
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
        //calcula la distancia entre el hombre izqueirdo y el centro, para poder luego seleccionar el area de accion.
        private void reescalar(Skeleton skel)
        {
            float ejeZ = skel.Joints[JointType.ShoulderCenter].Position.Z;
            if (ejeZ > ejeZDistacia + 0.5 || ejeZ < ejeZDistacia - 0.5)
            {
                distanciaX = skel.Joints[JointType.ShoulderCenter].Position.X - skel.Joints[JointType.ShoulderLeft].Position.X;
                ejeZDistacia = ejeZ;
                System.Console.Out.WriteLine("REEESCALANDO: " + ejeZ + " -> " + ejeZDistacia);
            }
        }
        //esta funcion hace demasiadas cosas :( hay que refactorizarla
        private void moverMano(Skeleton skel, DrawingContext dc)
        {
            Point punto = this.SkeletonPointToScreen(skel.Joints[JointType.HandLeft].Position);

            float manoIzquierdaX = skel.Joints[JointType.HandLeft].Position.X;
            //calculo la zona de accion del eje X para la zona izquierda.
            float inicioAccionX = (distanciaX * 3 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
            float finAccionX = (distanciaX * 5 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;

            float manoIzquierdaY = skel.Joints[JointType.HandLeft].Position.Y;

            float manoDerechaX = skel.Joints[JointType.HandRight].Position.X;
            //calculo la zona de accion del ejeX para la zona derecha
            float inicioAccionDerechaX = (distanciaX * 3 + skel.Joints[JointType.ShoulderCenter].Position.X);
            float finAccionDerechaX = (distanciaX * 5 + skel.Joints[JointType.ShoulderCenter].Position.X);

            float manoDerechaY = skel.Joints[JointType.HandRight].Position.Y;

            //mostrar(skel.Joints[JointType.HandLeft].Position);
            Point punto2 = this.SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position);
            Double manoDerechaZ = skel.Joints[JointType.HandRight].Position.Z;
            Double manoIzquierdaZ = skel.Joints[JointType.HandLeft].Position.Z;

            //calculo la zona de accion del ejeY para las zonas izquierda y derecha (como son simetricas vale para ambas.
            float inicioAccionY = skel.Joints[JointType.ShoulderCenter].Position.Y - distanciaX * 2;
            float finAccionY = skel.Joints[JointType.ShoulderCenter].Position.Y + distanciaX * 2;

            // fin de coger los datos para los lados derecha e izquierda

            //calculo la zona de accion del ejeX para la zona de arriba y la de abajo (me vale para ambas).
            float inicioAccionArribaX = (distanciaX * 2 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
            float finAccionArribaX = (distanciaX * 2 + skel.Joints[JointType.ShoulderCenter].Position.X);
            //calculo la zona de accion del ejeY para la zona de arriba.
            float inicioAccionArribaY = (distanciaX + skel.Joints[JointType.Head].Position.Y);
            float finAccionArribaY = (distanciaX * 3 + skel.Joints[JointType.Head].Position.Y);
            //calculo la zona de accion del ejeY para la zona de abajo.
            float inicioAccionAbajoY = (distanciaX * 2 - skel.Joints[JointType.ShoulderCenter].Position.Y) * -1;
            float finAccionAbajoY = (distanciaX * 4 - skel.Joints[JointType.ShoulderCenter].Position.Y) * -1;
            //como para sacar de un punto de kinect a una coordenada en pixeles necesito un SkeletonPoint lo creo.
            //genero en pixeles todas las zonas, para que el usuario vea cuales son las zonas activas.
            SkeletonPoint puntoMedio = new SkeletonPoint();
            puntoMedio.X = finAccionX;
            puntoMedio.Z = skel.Joints[JointType.ShoulderCenter].Position.Z;
            puntoMedio.Y = finAccionY;
            Point puntoFinIzquierda = SkeletonPointToScreen(puntoMedio);
            puntoMedio.X = inicioAccionX;
            puntoMedio.Z = skel.Joints[JointType.ShoulderCenter].Position.Z;
            puntoMedio.Y = inicioAccionY;
            Point puntoInicioIzquierda = SkeletonPointToScreen(puntoMedio);

            puntoMedio.X = inicioAccionDerechaX;
            Point puntoInicioDerecha = SkeletonPointToScreen(puntoMedio);
            puntoMedio.X = finAccionDerechaX;
            puntoMedio.Y = finAccionY;
            Point puntoFinDerecha = SkeletonPointToScreen(puntoMedio);
            puntoMedio.X = inicioAccionArribaX;
            puntoMedio.Y = inicioAccionArribaY;
            Point puntoInicioArriba = SkeletonPointToScreen(puntoMedio);
            puntoMedio.X = finAccionArribaX;
            puntoMedio.Y = finAccionArribaY;
            Point puntoFinArriba = SkeletonPointToScreen(puntoMedio);
            puntoMedio.X = inicioAccionArribaX;
            puntoMedio.Y = inicioAccionAbajoY;
            Point puntoInicioAbajo = SkeletonPointToScreen(puntoMedio);
            puntoMedio.X = finAccionArribaX;
            puntoMedio.Y = finAccionAbajoY;
            Point puntoFinAbajo = SkeletonPointToScreen(puntoMedio);

            //controlo par que cunado se mueva fuera de los margenes no pinte mas haya de ellos, ya que sino
            //se reescala todo para poder pintarlo, y da efectos raros.
            if (puntoFinIzquierda.X < 0)
            {
                puntoFinIzquierda.X = 0;
            } 
            if (puntoFinDerecha.X > 640)
            {
                puntoFinDerecha.X = 640;
            } 
            if (puntoFinArriba.Y < 0)
            {
                puntoFinArriba.Y = 0;
            } 
            if (puntoFinAbajo.Y > 480)
            {
                puntoFinAbajo.Y = 480;
            }
            //control de errores para el tamaño de los cuadrados, puede dar que sea 0 cuando se salen de los margenes, y no
            //tiene sentido pintarlos si no.
            if (puntoFinArriba.X - puntoInicioArriba.X > 0 && puntoInicioArriba.Y - puntoFinArriba.Y > 0)
            {
                dc.DrawRectangle(brushred, null, new Rect(puntoInicioArriba.X, puntoFinArriba.Y, puntoFinArriba.X - puntoInicioArriba.X, puntoInicioArriba.Y - puntoFinArriba.Y));
            }
            if (puntoFinArriba.X - puntoInicioArriba.X > 0 && puntoInicioIzquierda.Y - puntoFinIzquierda.Y > 0)
            {
                dc.DrawRectangle(brushred, null, new Rect(puntoInicioArriba.X, puntoInicioAbajo.Y, puntoFinArriba.X - puntoInicioArriba.X, puntoFinAbajo.Y - puntoInicioAbajo.Y));
            }
            if(puntoInicioIzquierda.X - puntoFinIzquierda.X > 0 && puntoInicioIzquierda.Y - puntoFinIzquierda.Y > 0){
                dc.DrawRectangle(brushred, null, new Rect(puntoFinIzquierda.X, puntoFinIzquierda.Y, puntoInicioIzquierda.X - puntoFinIzquierda.X, puntoInicioIzquierda.Y - puntoFinIzquierda.Y));
            }
            if (puntoFinDerecha.X - puntoInicioDerecha.X > 0 && puntoInicioIzquierda.Y - puntoFinIzquierda.Y > 0)
            {
                dc.DrawRectangle(brushred, null, new Rect(puntoInicioDerecha.X, puntoFinIzquierda.Y, puntoFinDerecha.X - puntoInicioDerecha.X, puntoInicioIzquierda.Y - puntoFinIzquierda.Y));
            }
            //empieza el control para ver si esta dentro de la zona de accion.
            //zona de la izquierda.
            if ((manoIzquierdaX <= inicioAccionX && manoIzquierdaY >= inicioAccionY && manoIzquierdaX >= finAccionX && manoIzquierdaY <= finAccionY))
            {
                if (!pulsadoIzquierda)
                {
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.NUMPAD3);
                    pulsadoIzquierda = true;
                }
                dc.DrawRectangle(brush, null, new Rect(puntoFinIzquierda.X, puntoFinIzquierda.Y, puntoInicioIzquierda.X - puntoFinIzquierda.X, puntoInicioIzquierda.Y - puntoFinIzquierda.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.NUMPAD3);
                pulsadoIzquierda = false;
            }
            //zona de arriba
            if ((manoIzquierdaX >= inicioAccionArribaX && manoIzquierdaY >= inicioAccionArribaY && manoIzquierdaX <= finAccionArribaX && manoIzquierdaY <= finAccionArribaY) ||
                (manoDerechaX >= inicioAccionArribaX && manoDerechaY >= inicioAccionArribaY && manoDerechaX <= finAccionArribaX && manoDerechaY <= finAccionArribaY))
            {
                if (!pulsadoArriba)
                {
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.NUMPAD1);
                    pulsadoArriba = true;
                }
                dc.DrawRectangle(brush, null, new Rect(puntoInicioArriba.X, puntoFinArriba.Y, puntoFinArriba.X - puntoInicioArriba.X, puntoInicioArriba.Y - puntoFinArriba.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.NUMPAD1);
                pulsadoArriba = false;
            }
            //zona derecha.
            if (manoDerechaX >= inicioAccionDerechaX && manoDerechaY >= inicioAccionY && manoDerechaX <= finAccionDerechaX && manoDerechaY <= finAccionY)
            {
                if (!pulsadoDerecha)
                {
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.NUMPAD2);
                    pulsadoDerecha = true;
                }
                dc.DrawRectangle(brush, null, new Rect(puntoInicioDerecha.X, puntoFinIzquierda.Y, puntoFinDerecha.X - puntoInicioDerecha.X, puntoInicioIzquierda.Y - puntoFinIzquierda.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.NUMPAD2);
                pulsadoDerecha = false;
            }
            //zona de abajo
            if ((manoIzquierdaX >= inicioAccionArribaX && manoIzquierdaY >= inicioAccionAbajoY && manoIzquierdaX <= finAccionArribaX && manoIzquierdaY <= finAccionAbajoY) ||
                (manoDerechaX >= inicioAccionArribaX && manoDerechaY <= inicioAccionAbajoY && manoDerechaX <= finAccionArribaX && manoDerechaY >= finAccionAbajoY))
            {
                if (!pulsadoAbajo)
                {
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.NUMPAD4);
                    pulsadoAbajo = true;
                }
                
                dc.DrawRectangle(brush, null, new Rect(puntoInicioArriba.X, puntoInicioAbajo.Y, puntoFinArriba.X - puntoInicioArriba.X, puntoFinAbajo.Y - puntoInicioAbajo.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(VirtualKeyCode.NUMPAD4);
                pulsadoAbajo = false;
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

            //control para que no se pueda dividir por 0.
            if (manoIzquierdaZ == 0)
            {
                manoIzquierdaZ = 1;
            } 
            if (manoDerechaZ == 0)
            {
                manoDerechaZ = 1;
            }

            //calcula el radio de las bolas que aparecen en la mano dependiendo de su distancia a la camara.
            double radioIzquierda = 10 / manoIzquierdaZ * 5;
            double radioDerecha = 10 / manoDerechaZ * 5;
            //se retoca el punto para que la bola se encuentr en el centro de la mano.
            Point puntoExactoIzquierda = new Point(punto.X, punto.Y + radioIzquierda / 2);
            Point puntoExactoDerecha = new Point(punto2.X, punto2.Y + radioDerecha / 2);
            //control para que cuando la mano se salga de la pantalla no se pinte nada, para las redimensiones del imageSource.
            if (puntoExactoDerecha.X + radioDerecha > 640)
            {
                radioDerecha = 0;
            }
            if (puntoExactoDerecha.Y + radioDerecha > 480)
            {
                radioDerecha = 0;
            }
            if (puntoExactoIzquierda.X - radioIzquierda < 0)
            {
                radioIzquierda = 0;
            }
            if (puntoExactoIzquierda.Y + radioIzquierda > 480)
            {
                radioIzquierda = 0;
            }
            //se pintan las pelotas de las manos
            dc.DrawEllipse(brushPelota, null, puntoExactoIzquierda, radioIzquierda, radioIzquierda);
            dc.DrawEllipse(brushPelota, null, puntoExactoDerecha, radioDerecha, radioDerecha);
        }

        private void ponergorrito(Skeleton skel, DrawingContext dc)
        {
            Point cabeza = SkeletonPointToScreen(skel.Joints[JointType.Head].Position);
            //en C# se coloca un elemento en la interfaz por medio de margenes, eso se hace con el objeto Thickness.
            sombrero.Margin = new Thickness(cabeza.X-100, cabeza.Y-130, 500 - cabeza.X, 340 - cabeza.Y);
            //dc.DrawRectangle(Brushes.Aqua, null, new Rect(cabeza.X, cabeza.Y, 100, 100));
        }
        //convierte un SkeletonPoint a un Point, es decir, pasa de las coordinadas de la camara a coordinadas en pixeles.
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);

            return new Point(depthPoint.X, depthPoint.Y);
        }
        //funcion para debug, se debe de borrar
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
            // si no hay sensor no se hace nada (control de errores)
            if (null == this.sensor)
            {
                return;
            }

            bool depthReceived = false;
            bool colorReceived = false;
            //comprobamos si ha recibido los pixeles del Depth
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (null != depthFrame)
                {
                    //copiamos esos pixeles a un array auxiliar.
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    depthReceived = true;
                }
            }
            //comprobamos si hemos recibido los pixeles del ColorStream.
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (null != colorFrame)
                {
                    // copiamos los pixeles a un array auxiliar.
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    colorReceived = true;
                }
            }

            if (true == depthReceived)
            {
                this.sensor.CoordinateMapper.MapDepthFrameToColorFrame(
                    DepthFormat,
                    this.depthPixels,
                    ColorFormat,
                    this.colorCoordinates);

                Array.Clear(this.playerPixelData, 0, this.playerPixelData.Length);

                //recorremos las filas y columnas del DepthStream.
                for (int y = 0; y < this.depthHeight; ++y)
                {
                    for (int x = 0; x < this.depthWidth; ++x)
                    {
                        // calculamos el index en el array de DepthStream
                        int depthIndex = x + (y * this.depthWidth);

                        DepthImagePixel depthPixel = this.depthPixels[depthIndex];
                        int player = depthPixel.PlayerIndex;

                        // Si el pixel pertenece al jugador, le ponemos opacidad, sino lo dejamos transparente
                        if (player > 0)
                        {
                            // retrieve the depth to color mapping for the current depth pixel
                            ColorImagePoint colorImagePoint = this.colorCoordinates[depthIndex];

                            // scale color coordinates to depth resolution
                            int colorInDepthX = colorImagePoint.X / this.colorToDepthDivisor;
                            int colorInDepthY = colorImagePoint.Y / this.colorToDepthDivisor;

                            //controlamos los pixeles para no escribir fuera del array.
                            if (colorInDepthX > 0 && colorInDepthX < this.depthWidth && colorInDepthY >= 0 && colorInDepthY < this.depthHeight)
                            {
                                // calculamos el pixel dentro del array del jugador
                                int playerPixelIndex = colorInDepthX + (colorInDepthY * this.depthWidth);

                                // le añadimos la opacidad.
                                this.playerPixelData[playerPixelIndex] = opaquePixelValue;

                                //compensamos el pixel, es decir, el que sabemos que es del jugador, cojemos el de la izquierda
                                //de sta manera aunque tengamos un pequeño error nos aseguramos de que cogemos al jugador entero.
                                this.playerPixelData[playerPixelIndex - 1] = opaquePixelValue;
                            }
                        }
                    }
                }
            }

            if (true == colorReceived)
            {
                // exribrimos los pixeles dentro del array
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
