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
using System.Xml.Serialization;
using System.Windows.Media;

namespace ARGIK
{
    /// <summary>
    /// Lógica de interacción para Configuracion.xaml
    /// </summary>
    public partial class Configuracion : Window
    {
        bool modoSentado;

        // Para sonidos de todo tipo
        private MediaPlayer mediaPlayer = new MediaPlayer();

        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        SkeletonDisplayManager skeletonDisplayManager;
        readonly ContextTracker contextTracker = new ContextTracker();
        KinectSensor kinectSensor;
        //Manejador de la camara
        BindableNUICamera nuiCamera;

        //Lista de esqueletos detectados
        private Skeleton[] skeletons;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuracion"/> class.
        /// </summary>
        public Configuracion(bool modoSentado)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-20.mp3", UriKind.Relative));
            mediaPlayer.Play();

            InitializeComponent();
            this.modoSentado = modoSentado;
            if (this.modoSentado == true)
                seatedMode.IsChecked = true;
            else
                seatedMode.IsChecked = false;
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
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
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
        /// Cierra la ventana
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs" /> instancia que contiene los datos del evento.</param>
        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (kinectSensor == null)
                return;

            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.ColorFrameReady += kinectRuntime_ColorFrameReady;

            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;

            kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            });
            kinectSensor.SkeletonFrameReady += kinectRuntime_SkeletonFrameReady;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);

            //Encender el sensor
            kinectSensor.Start();







            //Controla la elevacion de la camara con el slider de la GUI
            nuiCamera = new BindableNUICamera(kinectSensor);
            elevacionCamara.DataContext = nuiCamera;

            //Mostrar en pantalla la imagen a color
            kinectDisplay.DataContext = colorManager;


        }
        /// <summary>
        /// Se encarga de manejar los frames del esqueleto que llegan en tiempo real.
        /// Llama a la funcion correspondiente para realizar el seguimiento del esqueleto
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="SkeletonFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {



            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;


                frame.GetSkeletons(ref skeletons);

                //Si no hay esqueletos frente al sensor se deshabilitan opciones y se limpian los canvas
                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                {
                    //botonGrabarGesto.IsEnabled = false;
                    //botonGrabarGestoViejo.IsEnabled = false;
                    //gesturesCanvas.Children.Clear();
                    kinectCanvas.Children.Clear();
                    return;
                }
                ProcessFrame(frame);
            }

        }
        /// <summary>
        /// Dibuja el esqueleto y el punto de seguimiento en el frame actual.
        /// Inicializa la deteccion del gesto del Joint correspondiente.
        /// </summary>
        /// <param name="frame">The frame.</param>
        public void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            //JointType articulacion = verificarArticulacion(articulacion_gesto);

            //Si hay esqueletos en la lista
            if (frame.Skeletons.Length > 0)
            {
                //botonGrabarGesto.IsEnabled = true;
                //botonGrabarGestoViejo.IsEnabled = true;
                foreach (var skeleton in frame.Skeletons)
                {
                    if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                        continue;


                    contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
                    //stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Estable" : "Inestable");
                    if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                        continue;

                    foreach (Joint joint in skeleton.Joints)
                    {
                        if (kinectSensor != null)
                        {

                            if (joint.TrackingState != JointTrackingState.Tracked)
                                continue;

                            ////Si es la Joint seleccionada para detectar o generar el gesto inicializa la deteccion 

                            //if (joint.JointType == articulacion && grabando)
                            //    reconocedorGesto.Add(joint.Position, kinectSensor);

                            ////verifica si la mano está dentro del boton
                            //if (joint.JointType == JointType.HandRight)
                            //{
                            //    TrackearManoDerecha(joint);
                            //}

                            ////Si la Joint es la mano izquierda detecta el Swipe hacia izquierda o derecha
                            //else if (joint.JointType == JointType.HandLeft)
                            //{
                            //    //if (botonDeslizar.IsChecked == true)
                            //    //    deslizarManoIzquierda.Add(joint.Position, kinectSensor);

                            //    //Habilita (si esta activada en la GUI) el manejo del mouse con la mano izquierda
                            //    //if (controlMouse.IsChecked == true)
                            //}
                        }
                    }

                    //Inicializa las posturas
                    //algorithmicPostureRecognizer.TrackPostures(skeleton);
                    //templatePostureDetector.TrackPostures(skeleton);

                    //if (recordNextFrameForPosture)
                    //{
                    //    templatePostureDetector.AddTemplate(skeleton);
                    //}
                }

                //Dibuja el esqueleto en la GUI
                skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);
                //skeletonDisplayManager.Draw(frame.Skeletons, true);
                //stabilitiesList.ItemsSource = stabilities;

            }

        }
        public void kinectRuntime_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {

            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                colorManager.Update(frame);
            }
        }
        /// <summary>
        /// Se encarga de manejar los frames de profundidad que llegan en tiempo real.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="DepthImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
        public void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                depthManager.Update(frame);
            }
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Clean()
        {
            if (kinectSensor != null)
            {
                kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= kinectRuntime_ColorFrameReady;
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the Click event of the ATRAS control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ATRAS_Click(object sender, RoutedEventArgs e)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-21.mp3", UriKind.Relative));
            mediaPlayer.Play();

            MenuPrincipal menuPrincipal = new MenuPrincipal(modoSentado);
            this.Clean();
            menuPrincipal.Show();
            this.Close();
        }

        /// <summary>
        /// Handles the 1 event of the seatedMode_Checked control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void seatedMode_Checked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;
            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            this.modoSentado = true;
        }

        /// <summary>
        /// Desactiva el modo "sentado" de la aplicacion
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instancia que contiene los datos del evento.</param>
        public void seatedMode_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;
            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            this.modoSentado = false;
        }

    }
}