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

namespace GesturesViewer
{
    /// <summary>
    /// Ventana Principal de la aplicacion. 
    /// Se muestra el menu de comandos y la imagen del Kinect
    /// </summary>
    public partial class MainWindow
    {
        //Sensor del Kinect
        KinectSensor kinectSensor;
       
        

        //Joint que se trackea
        String jointSeleccionada;
        
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
        TextBlock nombreGesto = new TextBlock();

        //Trackeador del contexto
        readonly ContextTracker contextTracker = new ContextTracker();
        
        //Detector de la combinacion de gestos
        ParallelCombinedGestureDetector parallelCombinedGestureDetector;
        SerialCombinedGestureDetector serialCombinedGestureDetector;
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
        
        //Postura
        TemplatedPostureDetector templatePostureDetector;
        private bool recordNextFrameForPosture;
        
        //Mostrar la imagen de profundidad?
        bool displayDepth;


        //VER
        string circleKBPath;
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
        /// <param name="jointSeleccionada"></param>
        public MainWindow(String jointSeleccionada)
        {
            this.jointSeleccionada = jointSeleccionada;
            InitializeComponent();

        }

        
        /// <summary>
        /// Actua dependendiendo del estado del Kinect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// Inicializa la ventana, sus componentes e instancia las variables
        /// </summary>
        public void Initialize()
        {
            if (kinectSensor == null)
                return;

            audioManager = new AudioStreamManager(kinectSensor.AudioSource);
            audioBeamAngle.DataContext = audioManager;

            botonGrabar.Click += new RoutedEventHandler(botonGrabar_Clicked);
            botonGesto.Click += new RoutedEventHandler(botonGesto_Clicked);

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
            deslizarManoIzquierda.OnGestureDetected += OnGestureDetected;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);
            
            //Encender el sensor
            kinectSensor.Start();

            //Se añade el texto al grid para que muestre el nombre del texto
           
            nombreGesto.Text = "";
            LayoutRoot.Children.Add(nombreGesto);
            //Configura la deteccion de gestos y posturas

            CargarDetectorGestos();
            CargarDetectorPosturas();
           
            //Controla la elevacion de la camara con el slider de la GUI
            nuiCamera = new BindableNUICamera(kinectSensor);
            elevacionCamara.DataContext = nuiCamera;

            //Comandos que podran ser reconocidos por voz
            voiceCommander = new VoiceCommander("grabar", "parar", "grabar gesto","parar gesto");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();

            //Mostrar en pantalla la imagen a color
            kinectDisplay.DataContext = colorManager;

            //Deshabilitar el boton de deteccion hasta que no haya un esqueleto
            botonGrabarGesto.IsEnabled = false;
            }

        /// <summary>
        /// Se encarga de manejar los frames de profundidad que llegan en tiempo real.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    botonGrabarGesto.IsEnabled = false;
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
        /// <param name="frame"></param>
        /// <returns></returns>
        public void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            JointType articulacion = verificarJoint(jointSeleccionada);
            
            //Si hay esqueletos en la lista
            if (frame.Skeletons.Length > 0)
            {
                botonGrabarGesto.IsEnabled = true;
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

                            //parallelCombinedGestureDetector = new ParallelCombinedGestureDetector();
                            //parallelCombinedGestureDetector.OnGestureDetected += OnGestureDetected;
                            //parallelCombinedGestureDetector.Add(deslizarManoIzquierda);
                            //parallelCombinedGestureDetector.Add(reconocedorGesto);
                            serialCombinedGestureDetector = new SerialCombinedGestureDetector();
                            serialCombinedGestureDetector.OnGestureDetected += OnGestureDetected;
                            
                            serialCombinedGestureDetector.Add(deslizarManoIzquierda);
                            serialCombinedGestureDetector.Add(reconocedorGesto);
                          

                            if (joint.TrackingState != JointTrackingState.Tracked)
                                continue;

                            //Si es la Joint seleccionada para detectar o generar el gesto inicializa la deteccion 
                            if (joint.JointType == articulacion)
                            {
                                reconocedorGesto.Add(joint.Position, kinectSensor);

                            }

                            //verifica si la mano está dentro del boton
                            if (joint.JointType == JointType.HandRight)
                            {
                                TrackHand(joint);
                            }

                            //Si la Joint es la mano izquierda detecta el Swipe hacia izquierda o derecha
                            else if (joint.JointType == JointType.HandLeft)
                            {
                                if (botonDeslizar.IsChecked == true)
                                    deslizarManoIzquierda.Add(joint.Position, kinectSensor);

                                //Habilita (si esta activada en la GUI) el manejo del mouse con la mano izquierda
                                if (controlMouse.IsChecked == true)
                                    MouseController.Current.SetHandPosition(kinectSensor, joint, skeleton);
                            }
                        }
                    }

                    //Inicializa las posturas
                    algorithmicPostureRecognizer.TrackPostures(skeleton);
                    templatePostureDetector.TrackPostures(skeleton);

                    if (recordNextFrameForPosture)
                    {
                        templatePostureDetector.AddTemplate(skeleton);
                        recordNextFrameForPosture = false;
                    }
                }

                //Dibuja el esqueleto en la GUI
                skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);
                stabilitiesList.ItemsSource = stabilities;

            }

        }

        /// <summary>
        /// Devuelve la joint que selecciono el usuario para el seguimiento
        /// </summary>
        /// <param name="jointSeleccionada"></param>
        /// <returns></returns>
        /// 
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
        /// Cierra la ventana
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Clean()
        {
            if (deslizarManoIzquierda != null)
            {
                deslizarManoIzquierda.OnGestureDetected -= OnGestureDetected;
            }

            if (audioManager != null)
            {
                audioManager.Dispose();
                audioManager = null;
            }

            if (parallelCombinedGestureDetector != null)
            {
                parallelCombinedGestureDetector.Remove(deslizarManoIzquierda);
                parallelCombinedGestureDetector.Remove(reconocedorGesto);
                parallelCombinedGestureDetector = null;
            }

            CerrarDetectorGestos();

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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void replay_DepthImageFrameReady(object sender, ReplayDepthImageFrameReadyEventArgs e)
        {
            if (!displayDepth)
                return;

            depthManager.Update(e.DepthImageFrame);
        }

        /// <summary>
        /// Maneja los frames de color (RGB) de la repeticion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            if (displayDepth)
                return;

            colorManager.Update(e.ColorImageFrame);
        }

        /// <summary>
        /// Maneja los frames del esqueleto de la repeticion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            ProcessFrame(e.SkeletonFrame);
        }

        
         
        

        /// <summary>
        /// Activa la grabacion del gesto mediante el boton de la GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void botonGesto_Clicked(object sender, RoutedEventArgs e)
                  
        {
            if (reconocedorGesto.IsRecordingPath)
            {
                reconocedorGesto.EndRecordTemplate();
                botonGrabarGesto.Content = "Grabar Gesto";

            }
            else
            {
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                CargarDetectorGestos();
                reconocedorGesto.StartRecordTemplate();
                botonGrabarGesto.Content = "Pausar Grabacion";
            }
        }

        

        /// <summary>
        /// Activa la grabacion de la sesion mediante el boton de la GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public void botonGrabar_Clicked(object sender, RoutedEventArgs e)
 
        {
            if (botonGrabar.IsChecked)
            {
                DirectRecord(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kinectRecord" + Guid.NewGuid() + ".replay"));
            }
            else
            {
                StopRecord();
            }
            
        }

        /// <summary>
        /// Verifica si la mano derecha esta sobre alguno de los botones de la GUI con RA
        /// </summary>
        /// <param name="hand"></param>
        public void TrackHand(Joint hand)
        {
                botonGrabar.Visibility = System.Windows.Visibility.Visible;
                botonGesto.Visibility = System.Windows.Visibility.Visible;
               
                DepthImagePoint puntoMano = kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);
    
                var transform = botonGrabar.TransformToVisual(LayoutRoot);
                var transform2 = botonGesto.TransformToVisual(LayoutRoot);
                

                Point topLeftRojo = transform.Transform(new Point(0, 0));
                Point topLeftAzul = transform2.Transform(new Point(0, 0));;

                
                if ( Math.Abs(puntoMano.X -  (topLeftRojo.X + botonGrabar.Width))  < 30 && 
                           Math.Abs(puntoMano.Y  - topLeftRojo.Y)  < 30 )
                {
                    botonGrabar.Hovering();
                }
                else botonGrabar.Release();

                if (Math.Abs(puntoMano.X -  (topLeftAzul.X + botonGesto.Width))  < 30 && 
                           Math.Abs(puntoMano.Y  - topLeftAzul.Y)  < 30 )         
                {
                    botonGesto.Hovering();
                }
                else botonGesto.Release();

                               
            }
        

        /// <summary>
        /// Muestra la imagen de profundidad o RGB segun el usuario seleccione en la GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Depth_RGB_Click(object sender, RoutedEventArgs e)
        {
            displayDepth = !displayDepth;

            if (displayDepth)
            {
                viewButton.Content = "RGB";
                kinectDisplay.DataContext = depthManager;
            }
            else
            {
                viewButton.Content = "Profundidad";
                kinectDisplay.DataContext = colorManager;
            }
        }
        /// <summary>
        /// Activa deteccion del gesto deslizar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public void botonDeslizar_Checked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.DepthStream.Range = DepthRange.Near;
            kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;
        }

        /// <summary>
        /// Desactiva deteccion del gesto deslizar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void botonDeslizar_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;


            }

        /// <summary>
        /// Activa el modo  "sentado" de la aplicacion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public void seatedMode_Checked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
        }

        /// <summary>
        /// Desactiva el modo "sentado" de la aplicacion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void seatedMode_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
        }

        

    }
}
