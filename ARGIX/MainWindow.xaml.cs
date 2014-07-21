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
//using Coding4Fun.Toolkit.Controls;

using System.Windows.Shapes;

namespace GesturesViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        KinectSensor kinectSensor;
        //PARA BOTON
        List<Button> buttons;
        static HoverButton selected;
        
        


        bool detectando = false;
        SwipeGestureDetector swipeGestureRecognizer;

        TemplatedGestureDetector circleGestureRecognizer;
        
        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        AudioStreamManager audioManager;
        SkeletonDisplayManager skeletonDisplayManager;
        readonly ContextTracker contextTracker = new ContextTracker();
        EyeTracker eyeTracker;
        ParallelCombinedGestureDetector parallelCombinedGestureDetector;
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
        
        TemplatedPostureDetector templatePostureDetector;
        
        private bool recordNextFrameForPosture;
        bool displayDepth;

        string circleKBPath;
        string letterT_KBPath;

        KinectRecorder recorder;
        KinectReplay replay;

        BindableNUICamera nuiCamera;

        private Skeleton[] skeletons;

        VoiceCommander voiceCommander;
        

        public MainWindow()
        {
            InitializeComponent();
        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            circleKBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\circleKB.save");
            letterT_KBPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"data\t_KB.save");

            try
            {
                //listen to any status change for Kinects
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
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

        private void Initialize()
        {
            if (kinectSensor == null)
                return;

            audioManager = new AudioStreamManager(kinectSensor.AudioSource);
            audioBeamAngle.DataContext = audioManager;

            kinectButton.Click += new RoutedEventHandler(kinectButton_Clicked);
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

            swipeGestureRecognizer = new SwipeGestureDetector();
            swipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);
            //kinectButton.Click += new RoutedEventHandler(kinectButton_Clicked);
            kinectSensor.Start();

            LoadCircleGestureDetector();
            LoadLetterTPostureDetector();
           
            nuiCamera = new BindableNUICamera(kinectSensor);

            elevationSlider.DataContext = nuiCamera;

            voiceCommander = new VoiceCommander("grabar", "parar");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;

            StartVoiceCommander();

            kinectDisplay.DataContext = colorManager;

            }

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
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

        void kinectRuntime_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
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

        void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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

                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                    return;
                
                ProcessFrame(frame);
            }
            
        }

        void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            foreach (var skeleton in frame.Skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                //if (eyeTracker == null)
                //    eyeTracker = new EyeTracker(kinectSensor);

                //eyeTracker.Track(skeleton);

                contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
                stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Estable" : "Inestable");
                if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                    continue;

                //if (eyeTracker.IsLookingToSensor.HasValue && eyeTracker.IsLookingToSensor == false)
                //    continue;
                //CheckButton(kinectButton, boton);

                foreach (Joint joint in skeleton.Joints)
                {
                    
                    parallelCombinedGestureDetector = new ParallelCombinedGestureDetector();
                    parallelCombinedGestureDetector.OnGestureDetected += OnGestureDetected;
                    parallelCombinedGestureDetector.Add(swipeGestureRecognizer);
                    parallelCombinedGestureDetector.Add(circleGestureRecognizer);
        
                    if (joint.TrackingState != JointTrackingState.Tracked)
                        continue;

                    if (joint.JointType == JointType.HandRight && kinectSensor != null)
                    {
                        circleGestureRecognizer.Add(joint.Position, kinectSensor);
                        //PARA BOTON
                        TrackHand(joint);
                    }
                    else if (joint.JointType == JointType.HandLeft)
                    {
                        swipeGestureRecognizer.Add(joint.Position, kinectSensor);
                        if (controlMouse.IsChecked == true)
                            MouseController.Current.SetHandPosition(kinectSensor, joint, skeleton);
                    }
                }

                algorithmicPostureRecognizer.TrackPostures(skeleton);
                templatePostureDetector.TrackPostures(skeleton);
                

                
                
                if (recordNextFrameForPosture)
                {
                    templatePostureDetector.AddTemplate(skeleton);
                    recordNextFrameForPosture = false;
                }
            }
           

            skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);

            stabilitiesList.ItemsSource = stabilities;
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }

        private void Clean()
        {
            /*if (swipeGestureRecognizer != null)
            {
                swipeGestureRecognizer.OnGestureDetected -= OnGestureDetected;
            }*/

            if (audioManager != null)
            {
                audioManager.Dispose();
                audioManager = null;
            }

            if (parallelCombinedGestureDetector != null)
            {
                parallelCombinedGestureDetector.Remove(swipeGestureRecognizer);
                parallelCombinedGestureDetector.Remove(circleGestureRecognizer);
                parallelCombinedGestureDetector = null;
            }

            //CloseGestureDetector();

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

            if (eyeTracker != null)
            {
                eyeTracker.Dispose();
                eyeTracker = null;
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

        private void replayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Replay files|*.replay" };

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

        void replay_DepthImageFrameReady(object sender, ReplayDepthImageFrameReadyEventArgs e)
        {
            if (!displayDepth)
                return;

            depthManager.Update(e.DepthImageFrame);
        }

        void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            if (displayDepth)
                return;

            colorManager.Update(e.ColorImageFrame);
        }

        void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            ProcessFrame(e.SkeletonFrame);
        }

        
         
        //PARA BOTON

        
        //private bool isHandOver(Joint hand, HoverButton buttonslist)
        //{
        //    var handTopLeft = new Point(hand.Position.X, hand.Position.Y);
        //    var handX = handTopLeft.X ;
        //    var handY = handTopLeft.Y ;



        //    if (buttonslist != null)
        //    {
        //        Point targetTopLeft = new Point(Canvas.GetLeft(buttonslist), Canvas.GetTop(buttonslist));
        //        if (handX > targetTopLeft.X &&
        //            handX < targetTopLeft.X + buttonslist.Width &&
        //            handY > targetTopLeft.Y &&
        //            handY < targetTopLeft.Y + buttonslist.Height)
        //        {
        //            selected = buttonslist;
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        void botonGesto_Clicked(object sender, RoutedEventArgs e)
        {
            System.Console.WriteLine("AZUUUUUUUUUUUUL");

        }

        void kinectButton_Clicked(object sender, RoutedEventArgs e)
 
        {
            if (kinectButton.IsChecked)
            {
                DirectRecord(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kinectRecord" + Guid.NewGuid() + ".replay"));
            }
            else
            {
                StopRecord();
            }
            
        }

        private void TrackHand(Joint hand)
        {
                kinectButton.Visibility = System.Windows.Visibility.Visible;
                botonGesto.Visibility = System.Windows.Visibility.Visible;
                
                
                //kinectButton.ImageSource = "/images/RedButton-Hover.png";
                //kinectButton.ActiveImageSource = "/images/RedButton-Active.png";

                DepthImagePoint puntoMano = kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);

               
                
                var transform = kinectButton.TransformToVisual(LayoutRoot);
                var transform2 = botonGesto.TransformToVisual(LayoutRoot);
                

                Point topLeftRojo = transform.Transform(new Point(0, 0));
                Point topLeftAzul = transform2.Transform(new Point(0, 0));;

                //.Console.WriteLine(absolutePosition);

                
                //System.Console.WriteLine(absolutePosition2);

                //System.Console.WriteLine(puntoMano.Y + "Y");

                //System.Console.WriteLine(topLeftRojo.X + "BOTON X");
                //System.Console.WriteLine(topLeftRojo.Y + "BOTON Y");
                
                
                if ( Math.Abs(puntoMano.X -  (topLeftRojo.X + kinectButton.Width))  < 30 && 
                           Math.Abs(puntoMano.Y  - topLeftRojo.Y)  < 30 )
                {
                    kinectButton.Hovering();
                }
                else kinectButton.Release();

                if (Math.Abs(puntoMano.X -  (topLeftAzul.X + botonGesto.Width))  < 30 && 
                           Math.Abs(puntoMano.Y  - topLeftAzul.Y)  < 30 )         {
                    botonGesto.Hovering();
                }
                else botonGesto.Release();

            ////Donde dice Layout Root va el nombre del Grid
                //var handX = (int)((point.X * LayoutRoot.ActualWidth / kinectSensor.DepthStream.FrameWidth) -
                //    (kinectButton.ActualWidth / 2.0));
                //var handY = (int)((point.Y * LayoutRoot.ActualHeight / kinectSensor.DepthStream.FrameHeight) -
                //    (kinectButton.ActualHeight / 2.0));
                //Canvas.SetLeft(kinectButton, handX);
                //Canvas.SetTop(kinectButton, handY);
            //    if (isHandOver(hand,kinectButton)) 
                                
            }
        

        private void Button_Click(object sender, RoutedEventArgs e)
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

        private void nearMode_Checked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.DepthStream.Range = DepthRange.Near;
            kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;
        }

        private void nearMode_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.DepthStream.Range = DepthRange.Default;
            kinectSensor.SkeletonStream.EnableTrackingInNearRange = false;
        }

        private void seatedMode_Checked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
        }

        private void seatedMode_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (kinectSensor == null)
                return;

            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
        }

        private void elevationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

    }
}
