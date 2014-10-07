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
    /// Ventana Principal de la aplicacion.
    /// Se muestra el menu de comandos y la imagen del Kinect
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

        SerializableDictionary<string, List<string>> diccionario;
        
        //Sensor del Kinect
        KinectSensor kinectSensor;

        //Gesto de la mano izquierda
        SwipeGestureDetector deslizarManoIzquierda;

        //Gesto de la mano derecha
        TemplatedGestureDetector reconocedorGesto;
        
        //Manejadores de las imagenes a color, de profundidad y esqueleto
        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        SkeletonDisplayManager skeletonDisplayManager;

        //Manejador del audio
        AudioStreamManager audioManager;

        //Texto que se muestra en pantalla con el nombre del gesto
        TextBlock repeticionesDisplay = new TextBlock();

        //Trackeador del contexto
        readonly ContextTracker contextTracker = new ContextTracker();

        //SerialCombinedGestureDetector serialCombinedGestureDetector;
        
        //Postura
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
        // TemplatedPostureDetector templatePostureDetector;
        // private bool recordNextFrameForPosture;
        
        //Mostrar la imagen de profundidad?
        bool displayDepth;

        //VER
        //string circleKBPath;
        string letterT_KBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\abc.save");

        //Para grabar y repetir las sesiones
        KinectRecorder recorder;
        KinectReplay replay;

        //Manejador de la camara
        BindableNUICamera nuiCamera;

        //Lista de esqueletos detectados
        private Skeleton[] skeletons;

        //Para manejar los comandos por voz
        VoiceCommander voiceCommander;

        /// <summary>
        /// Constructor de la ventana principal
        /// </summary>
        public Medico()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the StatusChanged event of the Kinects control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="StatusChangedEventArgs" /> instance containing the event data.</param>
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
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
        /// Inicializa los elementos de la interfaz
        /// </summary>
        public void Initialize()
        {
            if (kinectSensor == null)
                return;

            //repitiendo_gesto = false;

            audioManager = new AudioStreamManager(kinectSensor.AudioSource);
            //audioBeamAngle.DataContext = audioManager;

            this.botonGrabarSesion.Click += new RoutedEventHandler(botonGrabar_Clicked);
            this.botonAzul.Click += new RoutedEventHandler(botonAzul_Clicked);
            this.botonNegro.Click += new RoutedEventHandler(botonArticulacion_Clicked);
            this.botonVerde.Click += new RoutedEventHandler(botonVerde_Clicked);

            //kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
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

            deslizarManoIzquierda = new SwipeGestureDetector();
            //deslizarManoIzquierda.OnGestureDetected += OnGestureDetected;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);
            
            //Encender el sensor
            kinectSensor.Start();

            //Se añade el texto al grid para que muestre el nombre del texto
           
            repeticionesDisplay.Text = "";
            LayoutRoot.Children.Add(repeticionesDisplay);
            //Configura la deteccion de gestos y posturas

            CargarDetectorGestos();
            CargarDetectorPosturas();
           
            //Controla la elevacion de la camara con el slider de la GUI
            nuiCamera = new BindableNUICamera(kinectSensor);
            //elevacionCamara.DataContext = nuiCamera;

            //Comandos que podran ser reconocidos por voz
            voiceCommander = new VoiceCommander("grabar gesto", "detener gesto");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();

            //Mostrar en pantalla la imagen a color
            kinectDisplay.DataContext = colorManager;

            //Deshabilitar el boton de deteccion hasta que no haya un esqueleto
            //botonGrabarGesto.IsEnabled = false;
            articulacion_gesto = "";
            }

        private void botonVerde_Clicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void botonAzul_Clicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

      
        /// <summary>
        /// Se encarga de manejar los frames de profundidad que llegan en tiempo real.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DepthImageFrameReadyEventArgs"/> instance containing the event data.</param>
        public void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ColorImageFrameReadyEventArgs"/> instance containing the event data.</param>
        public void kinectRuntime_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SkeletonFrameReadyEventArgs"/> instance containing the event data.</param>
        public void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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
                    //botonGrabarGesto.IsEnabled = false;
                    //botonGrabarGestoViejo.IsEnabled = false;
                    gesturesCanvas.Children.Clear();
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
            JointType articulacion = verificarJoint(articulacion_gesto);
            
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
                    stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Estable" : "Inestable");
                    if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                        continue;

                    foreach (Joint joint in skeleton.Joints)
                    {
                        if (kinectSensor != null)
                        {
                                                    
                            if (joint.TrackingState != JointTrackingState.Tracked)
                                continue;

                            //Si es la Joint seleccionada para detectar o generar el gesto inicializa la deteccion 
                            
                            if (joint.JointType == articulacion && grabando)
                                reconocedorGesto.Add(joint.Position, kinectSensor);

                            //verifica si la mano está dentro del boton
                            if (joint.JointType == JointType.HandRight)
                            {
                                TrackearManoDerecha(joint);
                            }

                            //Si la Joint es la mano izquierda detecta el Swipe hacia izquierda o derecha
                            else if (joint.JointType == JointType.HandLeft)
                            {
                                //if (botonDeslizar.IsChecked == true)
                                //    deslizarManoIzquierda.Add(joint.Position, kinectSensor);

                                //Habilita (si esta activada en la GUI) el manejo del mouse con la mano izquierda
                                //if (controlMouse.IsChecked == true)
                                //    MouseController.Current.SetHandPosition(kinectSensor, joint, skeleton);
                            }
                        }
                    }

                    //Inicializa las posturas
                    algorithmicPostureRecognizer.TrackPostures(skeleton);
                    //templatePostureDetector.TrackPostures(skeleton);
                
                    //if (recordNextFrameForPosture)
                    //{
                    //    templatePostureDetector.AddTemplate(skeleton);
                    //}
                }

                //Dibuja el esqueleto en la GUI
                //skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);
                skeletonDisplayManager.Draw(frame.Skeletons, true);
                //stabilitiesList.ItemsSource = stabilities;

            }

        }

        /// <summary>
        /// Devuelve la joint que selecciono el usuario para el seguimiento
        /// </summary>
        /// <param name="jointSeleccionada">The joint seleccionada.</param>
        /// <returns></returns>
        public JointType verificarJoint(String jointSeleccionada)
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
        /// Nuevoes the clean.
        /// </summary>
        public void nuevoClean()
        {
            if (deslizarManoIzquierda != null)
            {
                //deslizarManoIzquierda.OnGestureDetected -= OnGestureDetected;
            }

            if (audioManager != null)
            {
                audioManager.Dispose();
                audioManager = null;
            }

            ClosePostureDetector();

            if (voiceCommander != null)
            {
                voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
                voiceCommander.Stop();
            }
            if (recorder != null)
            {
                recorder.Stop();
            }
            if (kinectSensor != null)
            {
                kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= kinectRuntime_ColorFrameReady;
                kinectSensor.Stop();
            }
        }

        /// <summary>
        /// Nuevoes the start.
        /// </summary>
        public void nuevoStart()
        {
            //deslizarManoIzquierda.OnGestureDetected -= OnGestureDetected;

            //audioManager = new AudioStreamManager(kinectSensor.AudioSource);

            CargarDetectorGestos();

            CargarDetectorPosturas();

            voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();


            kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
            kinectSensor.SkeletonFrameReady += kinectRuntime_SkeletonFrameReady;
            kinectSensor.ColorFrameReady += kinectRuntime_ColorFrameReady;
            kinectSensor.Start();
            //this.articulacion_gesto = articulacion_gesto;
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

            ClosePostureDetector();

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
                kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= kinectRuntime_ColorFrameReady;
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }

        /// <summary>
        /// Abre la ventana del menu Replay. Permite seleccionar el archivo de sesion a repetir.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        public void replayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog {Title = "Select filename", Filter = "Replay files|*.replay" };
            if (openFileDialog.ShowDialog() == true)
            {
                if (replay != null)
                {
                    replay.SkeletonFrameReady -= replay_SkeletonFrameReady;
                    replay.ColorImageFrameReady -= replay_ColorImageFrameReady;
                    replay.Stop();
                }
                Stream recordStream = File.OpenRead(openFileDialog.FileName);
                replay = new KinectReplay(recordStream);
                replay.SkeletonFrameReady += replay_SkeletonFrameReady;
                replay.ColorImageFrameReady += replay_ColorImageFrameReady;
                replay.DepthImageFrameReady += replay_DepthImageFrameReady;
                replay.Start();
            }
        }

        /// <summary>
        /// Maneja los frames de profundidad de la repeticion
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ReplayDepthImageFrameReadyEventArgs" /> instance containing the event data.</param>
        public void replay_DepthImageFrameReady(object sender, ReplayDepthImageFrameReadyEventArgs e)
        {
            if (!displayDepth)
                return;
            depthManager.Update(e.DepthImageFrame);
        }

        /// <summary>
        /// Maneja los frames de color (RGB) de la repeticion
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ReplayColorImageFrameReadyEventArgs" /> instance containing the event data.</param>
        public void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            if (displayDepth)
                return;

            colorManager.Update(e.ColorImageFrame);
        }

        /// <summary>
        /// Maneja los frames del esqueleto de la repeticion
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ReplaySkeletonFrameReadyEventArgs" /> instance containing the event data.</param>
        public void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            ProcessFrame(e.SkeletonFrame);
        }

        /// <summary>
        /// Boton Rojo de RA, genera el XML con la lista.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        public void botonGrabar_Clicked(object sender, RoutedEventArgs e) 
        {
            if (this.botonGrabarSesion.IsChecked )
            {
                diccionario = new SerializableDictionary<string, List<string>>();
                List<string> medico = new List<string>();
                medico.Add("German Leschevich");
                List<string> paciente = new List<string>();
                paciente.Add("Gaston Diaz");
                List<string> precision = new List<string>();
                precision.Add("Media");
                List<string> gestos = new List<string>();


                diccionario.Add("Medico", medico);
                diccionario.Add("Paciente", paciente);
                diccionario.Add("Precision", precision);
                diccionario.Add("Gestos", gestos);
            }
            else
            {

                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextWriter textWriter = new StreamWriter(@"Gaston Diaz.xml");
                serializer.Serialize(textWriter, diccionario);
                textWriter.Close();
            }
            
        }

        /// <summary>
        /// Handles the Clicked event of the botonArticulacion control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void botonArticulacion_Clicked(object sender, RoutedEventArgs e)
        {
            //if (voiceCommander != null)
            //{
            //    voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
            //    voiceCommander.Stop();
            //    voiceCommander = null;
            //}
            //if (audioManager != null)
            //{
            //    audioManager.Dispose();
            //    audioManager = null;
            //}

            this.nuevoClean();
            //this.Hide();
            this.Visibility = Visibility.Collapsed;
            Articulaciones jointsNuevo = new Articulaciones(this);
            jointsNuevo.ShowDialog();
            
            this.Visibility = Visibility.Visible;
            this.nuevoStart();
            //this.Show();
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
                var transform2 = this.botonAzul .TransformToVisual(LayoutRoot);
                var transform3 = this.botonNegro.TransformToVisual(LayoutRoot);
                var transform4 = this.botonVerde.TransformToVisual(LayoutRoot);

                Point puntoBotonGrabarSesion = transform1.Transform(new Point(0, 0));
                Point puntoBotonReproducirSesion = transform2.Transform(new Point(0, 0));
                Point puntoBotonNegro = transform3.Transform(new Point(0, 0));
                Point puntoBotonVerde = transform4.Transform(new Point(0, 0));
                
                // Verifica si el punto trackeado esta sobre el boton
                if ( Math.Abs(puntoMano.X - (puntoBotonGrabarSesion.X + botonGrabarSesion.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonGrabarSesion.Y) < 30 )
                    botonGrabarSesion.Hovering();
                else 
                    botonGrabarSesion.Release();

                if (Math.Abs(puntoMano.X - (puntoBotonReproducirSesion.X + botonAzul.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonReproducirSesion.Y) < 30)
                    botonAzul.Hovering();
                else
                    botonAzul.Release();

                if (Math.Abs(puntoMano.X - (puntoBotonNegro.X + botonNegro.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonNegro.Y) < 30)
                    botonNegro.Hovering();
                else
                    botonNegro.Release();

                if (Math.Abs(puntoMano.X - (puntoBotonVerde.X + botonVerde.Width)) < 30 && Math.Abs(puntoMano.Y - puntoBotonVerde.Y) < 30)
                    botonVerde.Hovering();
                else
                    botonVerde.Release();
        }

        /// <summary>
        /// Muestra la imagen de profundidad o RGB segun el usuario seleccione en la GUI
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        public void Depth_RGB_Click(object sender, RoutedEventArgs e)
        {
            displayDepth = !displayDepth;
            if (displayDepth)
            {
                //viewButton.Content = "RGB";
                kinectDisplay.DataContext = depthManager;
            }
            else
            {
                //viewButton.Content = "Profundidad";
                kinectDisplay.DataContext = colorManager;
            }
        }

        /// <summary>
        /// Cierra la ventana
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs" /> instance containing the event data.</param>
        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }
    }
}
