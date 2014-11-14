using Microsoft.Kinect;
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

namespace ARGIK
{
    /// <summary>
    /// Lógica de interacción para MenuPrincipal.xaml
    /// </summary>
    public partial class MenuPrincipal : Window
    {

        bool modoSentado;

        private KinectSensor _Kinect;
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private Skeleton[] FrameSkeletons;

        List<Button> buttons;
        static Button selected;

        float handX;
        float handY;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuPrincipal"/> class.
        /// </summary>
        public MenuPrincipal()
        {
            this.modoSentado = true;
            InitializeComponent();
            InitializeButtons();
            this.WindowState = System.Windows.WindowState.Maximized;
            this.WindowStyle = System.Windows.WindowStyle.None;
            if (Generics.LoadingStatus == 0)
            {
                Generics.ResetHandPosition(kinectButton);
                kinectButton.Click += new RoutedEventHandler(kinectButton_Click);
                this.Loaded += MenuPrincipal_Loaded;
                Generics.LoadingStatus = 1;
            }   
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuPrincipal"/> class.
        /// </summary>
        public MenuPrincipal(bool modoSentado2)
        {
            this.modoSentado = modoSentado2;
            InitializeComponent();
            InitializeButtons();
            this.WindowState = System.Windows.WindowState.Maximized;
            this.WindowStyle = System.Windows.WindowStyle.None;
            if (Generics.LoadingStatus == 0)
            {
                Generics.ResetHandPosition(kinectButton);
                kinectButton.Click += new RoutedEventHandler(kinectButton_Click);
                this.Loaded += MenuPrincipal_Loaded;
                Generics.LoadingStatus = 1;
            }    
        }

        void MenuPrincipal_Loaded(object sender, RoutedEventArgs e)
        {
            DiscoverKinectSensor(); 
        } 
        #region "Hand Gesture"

        //initialize buttons to be checked
        private void InitializeButtons()
        {
            buttons = new List<Button> { GAME1, GAME2, GAME3, CERRAR };
        }
        //raise event for Kinect sensor status changed
        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;

            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        private void UnregisterEvents()
        {
            KinectSensor.KinectSensors.StatusChanged -= KinectSensors_StatusChanged;
            if (this._Kinect != null)
            {
                this._Kinect.SkeletonFrameReady -= Kinect_SkeletonFrameReady;
                this._Kinect.ColorFrameReady -= Kinect_ColorFrameReady;
                this._Kinect.Stop();
                this._Kinect = null;
            }
            if (this.Kinect != null)
            {
                this.Kinect.SkeletonFrameReady -= Kinect_SkeletonFrameReady;
                this.Kinect.ColorFrameReady -= Kinect_ColorFrameReady;
                this.Kinect.Stop();
                this.Kinect = null;
            }
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        this.Kinect = null;
                        this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.Kinect == null)
                        {
                            MessageBox.Show("El sensor esta desconectado. Por favor, conectelo de nuevo.");
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the kinect.
        /// </summary>
        /// <value>
        /// The kinect.
        /// </value>
        public KinectSensor Kinect
        {
            get { return this._Kinect; }
            set
            {
                if (this._Kinect != value)
                {
                    if (this._Kinect != null)
                    {
                        this._Kinect = null;
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this._Kinect = value;
                        InitializeKinectSensor(this._Kinect);
                    }
                }
            }
        }

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                ColorImageStream colorStream = kinectSensor.ColorStream;
                colorStream.Enable();
                this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                    96, 96, PixelFormats.Bgr32, null);
                this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                // videoStream.Source = this._ColorImageBitmap;

                kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters()
                {
                    Correction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f,
                    Smoothing = 0.5f
                });

                kinectSensor.SkeletonFrameReady += Kinect_SkeletonFrameReady;
                kinectSensor.ColorFrameReady += Kinect_ColorFrameReady;

                if (!kinectSensor.IsRunning)
                {
                    kinectSensor.Start();
                }

                this.FrameSkeletons = new Skeleton[this.Kinect.SkeletonStream.FrameSkeletonArrayLength];

            }
        }

        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData,
                        this._ColorImageStride, 0);
                }
            }
        }

        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this.FrameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this.FrameSkeletons);

                    if (skeleton == null)
                    {
                        kinectButton.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Joint primaryHand = GetPrimaryHand(skeleton);
                        TrackHand(primaryHand);

                    }
                }
            }
        }

        //track and display hand
        private void TrackHand(Joint hand)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                kinectButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                kinectButton.Visibility = System.Windows.Visibility.Visible;

                DepthImagePoint point = this.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);
                handX = (int)((point.X * LayoutRoot.ActualWidth / this.Kinect.DepthStream.FrameWidth) -
                    (kinectButton.ActualWidth / 2.0));
                handY = (int)((point.Y * LayoutRoot.ActualHeight / this.Kinect.DepthStream.FrameHeight) -
                    (kinectButton.ActualHeight / 2.0));
                Canvas.SetLeft(kinectButton, handX);
                Canvas.SetTop(kinectButton, handY);

                if (isHandOver(kinectButton, buttons)) kinectButton.Hovering();
                else kinectButton.Release();
                if (hand.JointType == JointType.HandRight)
                {
                    kinectButton.ImageSource = "/ARGIK;component/Images/myhand.png";
                    kinectButton.ActiveImageSource = "/ARGIK;component/Images/myhand.png";
                }
                else
                {
                    kinectButton.ImageSource = "/ARGIK;component/Images/myhand.png";
                    kinectButton.ActiveImageSource = "/ARGIK;component/Images/myhand.png";
                }
            }
        }

        //detect if hand is overlapping over any button
        private bool isHandOver(FrameworkElement hand, List<Button> buttonslist)
        {
            var handTopLeft = new Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
            var handX = handTopLeft.X + hand.ActualWidth / 2;
            var handY = handTopLeft.Y + hand.ActualHeight / 2;

            foreach (Button target in buttonslist)
            {

                if (target != null)
                {
                    Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                    if (handX > targetTopLeft.X &&
                        handX < targetTopLeft.X + target.Width &&
                        handY > targetTopLeft.Y &&
                        handY < targetTopLeft.Y + target.Height)
                    {
                        selected = target;
                        return true;
                    }
                }
            }
            return false;
        }

        //get the hand closest to the Kinect sensor
        private static Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();
            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint rightHand = skeleton.Joints[JointType.HandRight];
                if (rightHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                    {
                        primaryHand = rightHand;
                    }
                    else
                    {
                        if (primaryHand.Position.Z > rightHand.Position.Z)
                        {
                            primaryHand = rightHand;
                        }
                    }
                }
            }
            return primaryHand;
        }

        //get the skeleton closest to the Kinect sensor
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;
            if (skeletons != null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }

        void kinectButton_Click(object sender, RoutedEventArgs e)
        {
            selected.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, selected));
        }

        #endregion

        private void MEDICO_Click(object sender, RoutedEventArgs e)
        {
            UnregisterEvents();
            Medico medico = new Medico(modoSentado);
            this.Close();
            medico.Show();
        }
 
        private void PACIENTE_Click(object sender, RoutedEventArgs e)
        {
            UnregisterEvents();
            Paciente paciente = new Paciente(modoSentado);
            this.Close();
            paciente.Show();
        }

        private void CONFIGURACION_Click(object sender, RoutedEventArgs e)
        {
            UnregisterEvents();
            Configuracion configuracion = new Configuracion(modoSentado);
            this.Close();
            configuracion.Show();
        }
        
        private void CERRAR_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Clean(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                //kinectSensor.DepthFrameReady -= Kinect_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= Kinect_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= Kinect_ColorFrameReady;
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }
    }
}