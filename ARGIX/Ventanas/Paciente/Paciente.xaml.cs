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

namespace ARGIK
{
    /// <summary>
    /// Ventana del Paciente
    /// </summary>
    public partial class Paciente : Window
    {

        // bool detectando;

        bool modoSentado;
        bool ayudaHabilitada;
        bool sesionIniciada;

        // Diccionario que contiene los datos de los gestos
        SerializableDictionary<string, List<string>> diccionario;

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

        //Para grabar y repetir las sesiones
        KinectRecorder recorder;
        KinectReplay replay;

        //Lista de esqueletos detectados
        private Skeleton[] skeletons;

        //Para manejar los comandos por voz
        VoiceCommander voiceCommander;

        /// <summary>
        /// Initializes a new instance of the <see cref="Paciente"/> class.
        /// </summary>
        /// <param name="modoSentado">if set to <c>true</c> [seated mode].</param>
        public Paciente(bool modoSentado)
        {
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
        /// Handles the StatusChanged event of the Kinects control.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="StatusChangedEventArgs" /> instancia que contiene los datos del evento.</param>
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
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            // Verifica si el kinect esta conectado
            if (kinectSensor == null)
                return;

            // Desactica la carga inicial de gestos
            cargar_gesto = false;

            // Inicialización del sonido 
            audioManager = new AudioStreamManager(kinectSensor.AudioSource);

            // Inicializa los botones de la interfaz
            botonReproducirSesion.Click += new RoutedEventHandler(botonReproducirSesion_Clicked);
            botonRepetirGesto.Click += new RoutedEventHandler(botonRepetirGesto_Clicked);
            botonAyudaPaciente.Click += new RoutedEventHandler(botonAyudaPaciente_Clicked);
            botonVerdePaciente.Click += new RoutedEventHandler(botonVerdePaciente_Clicked);

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
            mensajePantalla.Foreground = new SolidColorBrush(Colors.Red);
            mensajePantalla.Text = "";
            mensajePantalla.Visibility = System.Windows.Visibility.Visible;
            LayoutRoot.Children.Add(mensajePantalla);
            
            //Configura la deteccion de gestos, posturas y ayuda
            CargarDetectorGestos();
            CargarDetectorPosturas();
            CargarAyudas();

            //Botones RA
            sesionIniciada = false;
            botonRepetirGesto.Visibility = Visibility.Hidden;
            botonVerdePaciente.Visibility = Visibility.Hidden;

            //Comandos que podran ser reconocidos por voz
            voiceCommander = new VoiceCommander("reproducir", "detener", "repetir", "ayuda", "salir");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();

            //Mostrar en pantalla la imagen a color
            kinectDisplayPaciente.DataContext = colorManager;

            articulacion_gesto = "";


            // Se chequea el modo sentado
            if (this.modoSentado == true)
            {
                kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            }
            else
            {
                kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }
        }

        /// <summary>
        /// Se encarga de manejar los frames de profundidad que llegan en tiempo real.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="DepthImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;
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
        /// <param name="e">The <see cref="ColorImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (replay != null && !replay.IsFinished)
                return;
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
        /// <param name="e">The <see cref="SkeletonFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            if (replay != null && !replay.IsFinished)
                return;
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;
                if (recorder != null && ((recorder.Options & KinectRecordOptions.Skeletons) != 0))
                    recorder.Record(frame);
                frame.GetSkeletons(ref skeletons);
                //Si no hay esqueletos frente al sensor se deshabilitan opciones y se limpian los canvas
                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                {
                    gesturesCanvas.Children.Clear();
                    kinectCanvas.Children.Clear();
                    return;
                }
                ProcessFrame(frame);
            }
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
            var transform1 = this.botonReproducirSesion.TransformToVisual(LayoutRoot);
            var transform2 = this.botonRepetirGesto.TransformToVisual(LayoutRoot);
            var transform3 = this.botonAyudaPaciente.TransformToVisual(LayoutRoot);
            var transform4 = this.botonVerdePaciente.TransformToVisual(LayoutRoot);

            Point puntoBotonReproducirSesion = transform1.Transform(new Point(0, 0));
            Point puntoBotonAzulPaciente = transform2.Transform(new Point(0, 0));
            Point puntoBotonNegroPaciente = transform3.Transform(new Point(0, 0));
            Point puntoBotonVerdePaciente = transform4.Transform(new Point(0, 0));

            // Verifica si el punto trackeado esta sobre el boton
            if (Math.Abs(puntoMano.X - (puntoBotonReproducirSesion.X + botonReproducirSesion.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonReproducirSesion.Y) < 30)
                botonReproducirSesion.Hovering();
            else
                botonReproducirSesion.Release();

            if (Math.Abs(puntoMano.X - (puntoBotonAzulPaciente.X + botonRepetirGesto.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonAzulPaciente.Y) < 30)
                botonRepetirGesto.Hovering();
            else
                botonRepetirGesto.Release();

            if (Math.Abs(puntoMano.X - (puntoBotonNegroPaciente.X + botonAyudaPaciente.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonNegroPaciente.Y) < 30)
                botonAyudaPaciente.Hovering();
            else
                botonAyudaPaciente.Release();

            if (Math.Abs(puntoMano.X - (puntoBotonVerdePaciente.X + botonVerdePaciente.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonVerdePaciente.Y) < 30)
                botonVerdePaciente.Hovering();
            else
                botonVerdePaciente.Release();
        }

        /// <summary>
        /// Dibuja el esqueleto y el punto de seguimiento en el frame actual.
        /// Inicializa la deteccion del gesto del Joint correspondiente.
        /// </summary>
        /// <param name="frame">The frame.</param>
        public void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            JointType articulacion = verificarArticulacion(articulacion_gesto);

            //Si hay esqueletos en la lista
            if (frame.Skeletons.Length > 0)
            {
                //botonGrabarGesto.IsEnabled = true;
                //botonGrabarGestoViejo.IsEnabled = true;
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

                            //Si es la Joint seleccionada para detectar o generar el gesto inicializa la deteccion 
                            if (joint.JointType == articulacion && replay != null && replay.IsFinished)
                            {
                                if (cargar_gesto == true)
                                {
                                    cargarGesto();
                                }
                                reconocedorGesto.Add(joint.Position, kinectSensor);
                            }

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
                //skeletonDisplayManager.Draw(frame.Skeletons, true);
                skeletonDisplayManager.Draw(frame.Skeletons, this.modoSentado);
            }

        }

        /// <summary>
        /// Devuelve la joint que selecciono el usuario para el seguimiento
        /// </summary>
        /// <param name="jointSeleccionada">The joint seleccionada.</param>
        /// <returns></returns>
        public JointType verificarArticulacion(String jointSeleccionada)
        {
            switch (jointSeleccionada)
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
            this.audioManager = new AudioStreamManager(kinectSensor.AudioSource);

            CargarDetectorGestos();
            CargarDetectorPosturas();

            this.voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();

            this.kinectSensor.DepthFrameReady += kinect_DepthFrameReady;
            this.kinectSensor.SkeletonFrameReady += kinect_SkeletonFrameReady;
            this.kinectSensor.ColorFrameReady += kinect_ColorFrameReady;
            this.kinectSensor.Start();
            
        }

        private void habilitarAyudas()
        {
            if (ayudaHabilitada == true)
            {
                
                ayudaIniciarSesion.Visibility = Visibility.Visible;
                ayudaIniciarSesionBorde.Visibility = Visibility.Visible;

                if (botonRepetirGesto.Visibility == Visibility.Visible)
                {
                    ayudaRepetir.Visibility = Visibility.Visible;
                    ayudaRepetirBorde.Visibility = Visibility.Visible;
                }

                ayudaAyuda.Visibility = Visibility.Visible;
                ayudaAyudaBorde.Visibility = Visibility.Visible;

                ayudaSalir.Visibility = Visibility.Visible;
                ayudaSalirBorde.Visibility = Visibility.Visible;
            }
            else
            {

                ayudaIniciarSesion.Visibility = Visibility.Collapsed;
                ayudaIniciarSesionBorde.Visibility = Visibility.Collapsed;
                ayudaRepetir.Visibility = Visibility.Collapsed;
                ayudaRepetirBorde.Visibility = Visibility.Collapsed;
                ayudaAyuda.Visibility = Visibility.Collapsed;
                ayudaAyudaBorde.Visibility = Visibility.Collapsed;
                ayudaSalir.Visibility = Visibility.Collapsed;
                ayudaSalirBorde.Visibility = Visibility.Collapsed;
            }
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

        /// <summary>
        /// Maneja los frames de profundidad de la repeticion
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="ReplayDepthImageFrameReadyEventArgs" /> instancia que contiene los datos del evento.</param>
        public void replay_DepthImageFrameReady(object sender, ReplayDepthImageFrameReadyEventArgs e)
        {
            if (!displayDepth)
                return;
            depthManager.Update(e.DepthImageFrame);
        }

        /// <summary>
        /// Maneja los frames de color (RGB) de la repeticion
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="ReplayColorImageFrameReadyEventArgs" /> instancia que contiene los datos del evento.</param>
        public void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            if (displayDepth)
                return;

            colorManager.Update(e.ColorImageFrame);
        }

        /// <summary>
        /// Maneja los frames del esqueleto de la repeticion
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="ReplaySkeletonFrameReadyEventArgs" /> instancia que contiene los datos del evento.</param>
        public void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            // ProcessFrame(e.SkeletonFrame);
            skeletonDisplayManager.Draw(e.SkeletonFrame.Skeletons, this.modoSentado);
        }

    }
}