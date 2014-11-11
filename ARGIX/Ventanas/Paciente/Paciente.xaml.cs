﻿using System;
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

        string nombre_gesto;
        int repeticion_gesto;
        bool repitiendo_gesto;
        string sesion_gesto;
        string articulacion_gesto;

        string letterT_KBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\abc.save");

        String archivoPostura = System.IO.Path.Combine(Environment.CurrentDirectory, @"..\..\gestos_posturas\t.save");

        bool detectando;

        SerializableDictionary<string, List<string>> diccionarioPaciente;

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

        //Detector de la combinacion de gestos
        ParallelCombinedGestureDetector parallelCombinedGestureDetector;

        SerialCombinedGestureDetector serialCombinedGestureDetector;

        //Postura
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
        TemplatedPostureDetector templatePostureDetector;
        private bool recordNextFrameForPosture;

        //Mostrar la imagen de profundidad?
        bool displayDepth;

        //Para grabar y repetir las sesiones
        KinectRecorder recorder;
        KinectReplay replay;

        //Manejador de la camara
        BindableNUICamera nuiCamera;

        //Lista de esqueletos detectados
        private Skeleton[] skeletons;

        //Para manejar los comandos por voz
        VoiceCommander voiceCommander;
        bool seatedMode = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Paciente"/> class.
        /// </summary>
        /// <param name="seatedMode">if set to <c>true</c> [seated mode].</param>
        public Paciente(bool seatedMode)
        {
            this.seatedMode = seatedMode;
            InitializeComponent();
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
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (kinectSensor == null)
                return;

            this.repitiendo_gesto = false;

            this.audioManager = new AudioStreamManager(kinectSensor.AudioSource);
            //audioBeamAngle.DataContext = audioManager;

            this.botonReproducirSesion.Click += new RoutedEventHandler(botonGesto_Clicked);
            this.botonAzulPaciente.Click += new RoutedEventHandler(botonAzulPaciente_Clicked);
            this.botonNegroPaciente.Click += new RoutedEventHandler(botonNegroPaciente_Clicked);
            this.botonVerdePaciente.Click += new RoutedEventHandler(botonRepetirGesto_Clicked);

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
            deslizarManoIzquierda.OnGestureDetected += OnGestureDetected;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);

            //Encender el sensor
            kinectSensor.Start();

            //Se añade el texto al grid para que muestre el nombre del texto

            repeticionesDisplay.Text = "";
            LayoutRootPaciente.Children.Add(repeticionesDisplay);
            //Configura la deteccion de gestos y posturas

            CargarDetectorGestos();
            CargarDetectorPosturas();

            //Controla la elevacion de la camara con el slider de la GUI
            nuiCamera = new BindableNUICamera(kinectSensor);
            //elevacionCamara.DataContext = nuiCamera;

            //Comandos que podran ser reconocidos por voz
            voiceCommander = new VoiceCommander("reproducir", "parar");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();

            //Mostrar en pantalla la imagen a color
            kinectDisplayPaciente.DataContext = colorManager;

            //Deshabilitar el boton de deteccion hasta que no haya un esqueleto
            //botonGrabarGesto.IsEnabled = false;
            articulacion_gesto = "";
        }

        /// <summary>
        /// Se encarga de manejar los frames de profundidad que llegan en tiempo real.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="DepthImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
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
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="ColorImageFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
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
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="SkeletonFrameReadyEventArgs"/> instancia que contiene los datos del evento.</param>
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
        /// Cargars the replay.
        /// </summary>
        public void cargarReplay()
        {
            detectando = false;
            grabando = false;

            repeticionesDisplay.Text = "";
            List<string> lista = new List<string>();
            if (diccionarioPaciente.TryGetValue("Gestos", out lista))
            {
                if (lista.Count != 0)
                {

                    for (int i = 0; i < (lista.Count); i++)
                    {
                        System.Console.WriteLine(lista[i] + "- Lista");
                    }

                    //frenar la deteccion de gestos
                    reconocedorGesto.OnGestureDetected -= OnGestureDetected;

                    //Guardar nombre de gesto, repeticiones y articulaciones
                    nombre_gesto = lista[0];
                    repeticion_gesto = Convert.ToInt32(lista[1]);
                    articulacion_gesto = lista[2];
                    sesion_gesto = lista[3];

                    //Mostrar repeticiones
                    repeticionesDisplay.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    repeticionesDisplay.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    repeticionesDisplay.FontSize = 75;
                    repeticionesDisplay.Margin = new Thickness(30, 0, 0, 0);
                    repeticionesDisplay.Foreground = new SolidColorBrush(Colors.Red);

                    //Repetir la sesion del gesto grabado 
                    if (replay != null)
                    {
                        replay.SkeletonFrameReady -= replay_SkeletonFrameReady;
                        replay.ColorImageFrameReady -= replay_ColorImageFrameReady;
                        replay.Stop();
                    }

                    Stream recordStreamReplay = File.OpenRead(sesion_gesto);
                    replay = new KinectReplay(recordStreamReplay);

                    //replay.SkeletonFrameReady += replay_SkeletonFrameReady;
                    replay.ColorImageFrameReady += replay_ColorImageFrameReady;
                    //replay.DepthImageFrameReady += replay_DepthImageFrameReady;


                    reconocedorGesto.DisplayCanvas = gesturesCanvas;
                    replay.Start();

                    repeticionesDisplay.Text = "DEMO";
                    repeticionesDisplay.Visibility = System.Windows.Visibility.Visible;

                }
            }
        }
        /// <summary>
        /// Verifica si la mano derecha esta sobre alguno de los botones de la GUI con RA
        /// </summary>
        /// <param name="hand">The hand.</param>
        public void TrackHand(Joint hand)
        {
            // Recupera el punto de la mano
            DepthImagePoint puntoMano = kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);

            // Recupera la posición de los botones
            var transform1 = this.botonReproducirSesion.TransformToVisual(LayoutRootPaciente);
            var transform2 = this.botonAzulPaciente.TransformToVisual(LayoutRootPaciente);
            var transform3 = this.botonNegroPaciente.TransformToVisual(LayoutRootPaciente);
            var transform4 = this.botonVerdePaciente.TransformToVisual(LayoutRootPaciente);

            Point puntobotonReproducirSesion = transform1.Transform(new Point(0, 0));
            Point puntobotonAzulPaciente = transform2.Transform(new Point(0, 0));
            Point puntobotonNegroPaciente = transform3.Transform(new Point(0, 0));
            Point puntobotonVerdePaciente = transform4.Transform(new Point(0, 0));

            // Verifica si el punto trackeado esta sobre el boton
            if (Math.Abs(puntoMano.X - (puntobotonReproducirSesion.X + botonReproducirSesion.Width)) < 30 && Math.Abs(puntoMano.Y - puntobotonReproducirSesion.Y) < 30)
                botonReproducirSesion.Hovering();
            else
                botonReproducirSesion.Release();

            if (Math.Abs(puntoMano.X - (puntobotonAzulPaciente.X + botonAzulPaciente.Width)) < 30 && Math.Abs(puntoMano.Y - puntobotonAzulPaciente.Y) < 30)
                botonAzulPaciente.Hovering();
            else
                botonAzulPaciente.Release();

            if (Math.Abs(puntoMano.X - (puntobotonNegroPaciente.X + botonNegroPaciente.Width)) < 30 && Math.Abs(puntoMano.Y - puntobotonNegroPaciente.Y) < 30)
                botonNegroPaciente.Hovering();
            else
                botonNegroPaciente.Release();

            if (Math.Abs(puntoMano.X - (puntobotonVerdePaciente.X + botonVerdePaciente.Width)) < 30 && Math.Abs(puntoMano.Y - puntobotonVerdePaciente.Y) < 30)
                botonVerdePaciente.Hovering();
            else
                botonVerdePaciente.Release();
        }

        /// <summary>
        /// Muestra la imagen de profundidad o RGB segun el usuario seleccione en la GUI
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instancia que contiene los datos del evento.</param>
        public void Depth_RGB_Click(object sender, RoutedEventArgs e)
        {
            displayDepth = !displayDepth;
            if (displayDepth)
            {
                //viewButton.Content = "RGB";
                kinectDisplayPaciente.DataContext = depthManager;
            }
            else
            {
                //viewButton.Content = "Profundidad";
                kinectDisplayPaciente.DataContext = colorManager;
            }
        }
        /// <summary>
        /// Activa el modo  "sentado" de la aplicacion
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instancia que contiene los datos del evento.</param>
        public void seatedMode_Checked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;
            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
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
                            if (joint.JointType == articulacion && replay != null && replay.IsFinished)
                            {
                                if (detectando == false && repitiendo_gesto == false)
                                    cargarGesto();
                                reconocedorGesto.Add(joint.Position, kinectSensor);
                            }
                            //if (joint.JointType == articulacion && grabando)
                            //    reconocedorGesto.Add(joint.Position, kinectSensor);

                            //verifica si la mano está dentro del boton
                            if (joint.JointType == JointType.HandRight)
                            {
                                TrackHand(joint);
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
                skeletonDisplayManager.Draw(frame.Skeletons, seatedMode);
                //skeletonDisplayManager.Draw(frame.Skeletons, true);
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
        public void nuevoClean()
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
            }

            CerrarDetectorGestos();

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

        public void nuevoStart()
        {
            deslizarManoIzquierda.OnGestureDetected -= OnGestureDetected;

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
            ProcessFrame(e.SkeletonFrame);
        }
    }
}