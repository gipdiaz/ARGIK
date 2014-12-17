using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using System.IO;
using Microsoft.Kinect;
using Microsoft.Win32;
using Kinect.Toolbox.Voice;
using System.Windows.Controls;
using Coding4Fun.Kinect.Wpf.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Windows.Controls.Primitives;

namespace ARGIK
{
    /// <summary>
    /// Ventana del Medico
    /// </summary>
    public partial class Medico : Window
    {

        /// <summary>
        /// Get o set articulacion_gesto.
        /// </summary>
        /// <value>
        /// Articulacion a trakear
        /// </value>
        public string articulacion_gesto { get; set; }

        bool modoSentado;
        bool ayudaHabilitada;
        bool sesionIniciada;
        bool vozHabilitada;
        
        // Diccionario que contiene los datos de los gestos
        SerializableDictionary<string, List<string>> diccionario;

        // Para sonidos de todo tipo
        private MediaPlayer mediaPlayer = new MediaPlayer();

        //Sensor del Kinect
        KinectSensor kinectSensor;

        // Reconocedor del gesto
        TemplatedGestureDetector reconocedorGesto;

        //Manejadores de las imagenes a color, de profundidad y esqueleto
        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        SkeletonDisplayManager skeletonDisplayManager;

        //Manejador del audio
        AudioStreamManager audioManager;

        //Texto que se muestra en pantalla con el nombre del gesto
        TextBlock mensajePantalla = new TextBlock();

        //Trackeador del contexto
        readonly ContextTracker contextTracker = new ContextTracker();

        //Postura
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();

        //Mostrar la imagen de profundidad?
        bool displayDepth = false;

        //Para grabar las sesiones
        KinectRecorder recorder;

        //Lista de esqueletos detectados
        private Skeleton[] skeletons;

        //Para manejar los comandos por voz
        VoiceCommander voiceCommander;

        /// <summary>
        /// Constructor de la ventana principal
        /// </summary>
        public Medico(bool modoSentado)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-20.mp3", UriKind.Relative));
            mediaPlayer.Play();

            this.modoSentado = modoSentado;
            InitializeComponent();
        }

        /// <summary>
        /// Inicializa el sensor
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instancia que contiene los datos del evento.</param>
        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //Controla los eventos que tienen que ver con el cambio de estado del sensor
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

                //Busca los sensores conectados y acciona el que ya se encuentra listo
                foreach (KinectSensor kinect in KinectSensor.KinectSensors)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinectSensor = kinect;
                        break;
                    }
                }
                if (KinectSensor.KinectSensors.Count == 0)
                    MessageBox.Show("No se encontro un Kinect conectado");
                else
                    Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        ///Controla los eventos que tienen que ver con el cambio de estado del sensor
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">La <see cref="StatusChangedEventArgs" /> instancia contiene los datos del evento.</param>
        public void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (kinectSensor == null)
                    {
                        kinectSensor = e.Sensor;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("El Kinect fue desconectado");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("El Kinect no tiene energía");
                    }
                    break;
                default:
                    MessageBox.Show("Estado inmanejable: " + e.Status);
                    break;
            }
        }


        /// <summary>
        /// Inicializa los elementos de la interfaz
        /// </summary>
        public void Initialize()
        {
            // Verifica si el kinect esta conectado
            if (kinectSensor == null)
                return;

            // Inicialización del sonido 
            this.audioManager = new AudioStreamManager(kinectSensor.AudioSource);

            // Inicializa los botones de la interfaz
            this.botonGrabarSesion.Click += new RoutedEventHandler(botonGrabar_Clicked);
            this.botonSeleccionarArticulacion.Click += new RoutedEventHandler(botonSeleccionarArticulacion_Clicked);
            this.botonAyuda.Click += new RoutedEventHandler(botonAyuda_Clicked);

            
            
            // Inicializa la camara RGB, la de profundidad y el esqueleto
            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.ColorFrameReady += kinect_ColorFrameReady;
            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinectSensor.DepthFrameReady += kinect_DepthFrameReady;
            kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            });
            kinectSensor.SkeletonFrameReady += kinect_SkeletonFrameReady;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);

            // Encender el sensor
            kinectSensor.Start();

            // Se añade el texto al grid para que muestre el nombre del texto
            //mensajePantalla.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            //mensajePantalla.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            //mensajePantalla.FontSize = 75;
            //mensajePantalla.Margin = new Thickness(30, 0, 0, 0);
            mensajePantalla.Foreground = new SolidColorBrush(Colors.DarkGreen);
            mensajePantalla.Text = "";
            LayoutRoot.Children.Add(mensajePantalla);
            
            //Configura la deteccion de gestos y posturas
            CargarDetectorGestos();
            CargarDetectorPosturas();
           
            //Se definen las ayudas de cada boton
            CargarAyudas();
            //Variables para voz
            sesionIniciada = false;
               
            //Botones RA
            botonSeleccionarArticulacion.Visibility = Visibility.Hidden;
            botonGrabarSesion.Visibility = Visibility.Hidden;
            
            botonAyuda.Visibility = Visibility.Hidden;
            //Comandos que podran ser reconocidos por voz
            voiceCommander = new VoiceCommander("grabar", "detener", "salir", "info");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();

            //Mostrar en pantalla la imagen a color
            kinectDisplay.DataContext = colorManager;

            articulacion_gesto = "Mano Derecha";

            // Se chequea el modo sentado
            if (this.modoSentado == true)
            {
                kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            }
            else
            {
                kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }

            nroGesto = 0;
        
        }

        private void habilitarAyudas()
        {
            if (ayudaHabilitada == true)
            {

                if (botonGrabarSesion.Visibility == Visibility.Visible)
                {
                    ayudaGrabarSesion.Visibility = Visibility.Visible;
                    ayudaGrabarSesionBorde.Visibility = Visibility.Visible;
                }
                if (botonSeleccionarArticulacion.Visibility == Visibility.Visible)
                {
                    ayudaArticulaciones.Visibility = Visibility.Visible;
                    ayudaArticulacionesBorde.Visibility = Visibility.Visible;
                }
                if (botonAyuda.Visibility == Visibility.Visible)
                {
                    ayudaAyuda.Visibility = Visibility.Visible;
                    ayudaAyudaBorde.Visibility = Visibility.Visible;
                }

                ayudaSalir.Visibility = Visibility.Visible;
                ayudaSalirBorde.Visibility = Visibility.Visible;
                ayudaVoz.Visibility = Visibility.Visible;
                ayudaVozBorde.Visibility = Visibility.Visible;
                comandosVoz.Visibility = Visibility.Visible;
            }
            else
            {
                ayudaGrabarSesion.Visibility = Visibility.Collapsed;
                ayudaGrabarSesionBorde.Visibility = Visibility.Collapsed;
                ayudaArticulaciones.Visibility = Visibility.Collapsed;
                ayudaArticulacionesBorde.Visibility = Visibility.Collapsed;
                ayudaAyuda.Visibility = Visibility.Collapsed;
                ayudaAyudaBorde.Visibility = Visibility.Collapsed;
                ayudaSalir.Visibility = Visibility.Collapsed;
                ayudaSalirBorde.Visibility = Visibility.Collapsed;
                ayudaVoz.Visibility = Visibility.Collapsed;
                ayudaVozBorde.Visibility = Visibility.Collapsed;
                comandosVoz.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Se encarga de manejar los frames de profundidad que llegan en tiempo real.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">La <see cref="DepthImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;
                if (recorder != null && ((recorder.Options & KinectRecordOptions.Depth) != 0))
                {
                    recorder.Record(frame);
                }
                if (!displayDepth)
                    return;
                depthManager.Update(frame);
            }
        }

        /// <summary>
        /// Se encarga de manejar los frames a color (RGB) que llegan en tiempo real.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">La <see cref="ColorImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                if (recorder != null && ((recorder.Options & KinectRecordOptions.Color) != 0))
                {
                    recorder.Record(frame);
                }

                if (displayDepth)
                    return;

                colorManager.Update(frame);
            }
        }

        /// <summary>
        /// Se encarga de manejar los frames del esqueleto que llegan en tiempo real.
        /// Llama a la funcion correspondiente para realizar el seguimiento del esqueleto
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">La <see cref="SkeletonFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;
                if (recorder != null && ((recorder.Options & KinectRecordOptions.Skeletons) != 0))
                    this.recorder.Record(frame);
                frame.GetSkeletons(ref skeletons);
                //Si no hay esqueletos frente al sensor se deshabilitan opciones y se limpian los canvas
                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                {
                    botonGrabarSesion.Visibility = Visibility.Hidden;
                    botonSeleccionarArticulacion.Visibility = Visibility.Hidden;
                    botonAyuda.Visibility = Visibility.Hidden;
                    ayudaHabilitada = false;
                    habilitarAyudas();
                    vozHabilitada = false;
                    this.gesturesCanvas.Children.Clear();
                    this.kinectCanvas.Children.Clear();
                    return;
                }
                else
                    vozHabilitada = true;
                ProcessFrame(frame);
            }
        }
        /// <summary>
        /// Metodo principal que analiza cada frame, en el cual se procesan los gestos y las posturas.
        /// </summary>
        /// <param name="frame">El frame.</param>
        public void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            JointType articulacion = verificarArticulacion(articulacion_gesto);

            //Si hay esqueletos en la lista
            if (frame.Skeletons.Length > 0)
            {
                botonGrabarSesion.Visibility = Visibility.Visible;
                if (sesionIniciada)
                    botonSeleccionarArticulacion.Visibility = Visibility.Visible;
                botonAyuda.Visibility = Visibility.Visible;
                foreach (var skeleton in frame.Skeletons)
                {
                    if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                        continue;

                    // Se analiza el esqueleto y se verifica si el mismo es válido
                    contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
                    stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Estable" : "Inestable");
                    if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                        continue;

                    // Se analizan todas las articulaciones de esqueleto
                    foreach (Joint joint in skeleton.Joints)
                    {
                        if (kinectSensor != null)
                        {
                            if (joint.TrackingState != JointTrackingState.Tracked)
                                continue;

                            // Verifica si la articulación actual es la articulación seleccionada y si se esta grabando la sesión. En tal caso se inicia la detección del gesto
                            if (joint.JointType == articulacion && grabando)
                                reconocedorGesto.Add(joint.Position, kinectSensor);

                            // Verifica si la articulación actual es la mano derecha
                            if (joint.JointType == JointType.HandRight)
                            {
                                TrackearManoDerecha(joint);
                            }
                        }
                    }

                    //Trakea las posturas
                    algorithmicPostureRecognizer.TrackPostures(skeleton);
                }

                //Dibuja el esqueleto en la GUI
                //skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);
                skeletonDisplayManager.Draw(frame.Skeletons, this.modoSentado);
            }

        }

        /// <summary>
        /// Devuelve la articulación que selecciono el usuario para el seguimiento
        /// </summary>
        /// <param name="articulacion">La articulación seleccionada</param>
        /// <returns></returns>
        public JointType verificarArticulacion(String articulacion)
        {
            
            switch (articulacion)
            {
                case "Cabeza": return JointType.Head;
                case "Mano Derecha": return JointType.HandRight;
                case "Mano Izquierda": return JointType.HandLeft;
                case "Muñeca Derecha": return JointType.WristRight;
                case "Muñeca Izquierda": return JointType.WristLeft;
                case "Rodilla Derecha": return JointType.KneeRight;
                case "Rodilla Izquierda": return JointType.KneeLeft;
                case "Pie Derecho": return JointType.FootRight;
                case "Pie Izquierdo": return JointType.FootLeft;
                default: return JointType.HandRight;
            }
        }


        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Clean()
        {
            if (audioManager != null)
            {
                audioManager.Dispose();
                audioManager = null;
            }

            CerrarDetectorPosturas();

            if (voiceCommander != null)
            {
                voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
                voiceCommander.Stop();
                voiceCommander = null;
            }
            if (recorder != null)
            {
                recorder.Stop();
                recorder = null;
            }
            if (kinectSensor != null)
            {
                kinectSensor.DepthFrameReady -= kinect_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= kinect_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= kinect_ColorFrameReady;
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }

        /// <summary>
        /// Reanudar el kinect
        /// </summary>
        public void Reanudar()
        {

            foreach (KinectSensor kinect in KinectSensor.KinectSensors)
            {
                if (kinect.Status == KinectStatus.Connected)
                {
                    kinectSensor = kinect;
                    break;
                }
            }
            //this.kinectSensor.DepthFrameReady += kinect_DepthFrameReady;
            //this.kinectSensor.SkeletonFrameReady += kinect_SkeletonFrameReady;
            //this.kinectSensor.ColorFrameReady += kinect_ColorFrameReady;
            this.kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            this.kinectSensor.ColorFrameReady += kinect_ColorFrameReady;
            this.kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            this.kinectSensor.DepthFrameReady += kinect_DepthFrameReady;
            this.kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            });
            this.kinectSensor.SkeletonFrameReady += kinect_SkeletonFrameReady;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);
            this.kinectSensor.Start();

            this.audioManager = new AudioStreamManager(kinectSensor.AudioSource);
           
            CargarDetectorGestos();
            CargarDetectorPosturas();

            voiceCommander = new VoiceCommander("grabar", "detener", "salir", "ayuda");
            this.voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();
            this.articulacion_gesto = articulacion_gesto;
            
        }

        /// <summary>
        /// Verifica si la mano derecha esta sobre alguno de los botones de la GUI con RA
        /// </summary>
        /// <param name="hand">The hand.</param>
        public void TrackearManoDerecha(Joint hand)
        {
            // Recupera el punto de la mano
            DepthImagePoint puntoMano = kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);

            // Recupera la posición de los botones
            var transform1 = this.botonGrabarSesion.TransformToVisual(LayoutRoot);
            var transform2 = this.botonAyuda.TransformToVisual(LayoutRoot);
            var transform3 = this.botonSeleccionarArticulacion.TransformToVisual(LayoutRoot);

            Point puntoBotonGrabarSesion = transform1.Transform(new Point(0, 0));
            Point puntoBotonSeleccionarArticulacion = transform2.Transform(new Point(0, 0));
            Point puntoBotonAyuda = transform3.Transform(new Point(0, 0));

            // Verifica si el punto trackeado esta sobre el boton
            if (Math.Abs(puntoMano.X - (puntoBotonGrabarSesion.X + botonGrabarSesion.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonGrabarSesion.Y) < 30 && botonGrabarSesion.Visibility == Visibility.Visible)
                botonGrabarSesion.Hovering();
            else
                botonGrabarSesion.Release();

            if (Math.Abs(puntoMano.X - (puntoBotonSeleccionarArticulacion.X + botonAyuda.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonSeleccionarArticulacion.Y) < 30 && botonAyuda.Visibility == Visibility.Visible)
                botonAyuda.Hovering();
            else
                botonAyuda.Release();

            if (Math.Abs(puntoMano.X - (puntoBotonAyuda.X + botonSeleccionarArticulacion.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonAyuda.Y) < 30 && botonSeleccionarArticulacion.Visibility == Visibility.Visible)
                botonSeleccionarArticulacion.Hovering();
            else
                botonSeleccionarArticulacion.Release();
        }
        


        /// <summary>
        /// Cierra la ventana
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs" /> instancia que contiene los datos del evento.</param>
        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }
    }
}