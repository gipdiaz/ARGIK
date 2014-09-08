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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor Kinect;
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private Skeleton[] FrameSkeletons;

        List<Button> buttons;
        static Button selected;

        float handX;
        float handY;
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
            this.WindowStyle = System.Windows.WindowStyle.None;

            if (Generics.LoadingStatus == 0)
            {
                _mainFrame.Source = new Uri("MainMenu.xaml", UriKind.Relative);
                Generics.LoadingStatus = 1;
            }
            kinectButton.Click += new RoutedEventHandler(kinectButton_Click); 
        }
        void kinectButton_Click(object sender, RoutedEventArgs e)
        {
            selected.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, selected));
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
        //detect if hand is overlapping over any button 
        private bool manoSobreBoton (FrameworkElement hand, List<Button> buttonslist) 
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
       
        //track and display hand 
        private void seguimientoMano(Joint hand)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                kinectButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                kinectButton.Visibility = System.Windows.Visibility.Visible;

                DepthImagePoint point = this.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);
                var handX = (int)((point.X * LayoutRoot.ActualWidth / this.Kinect.DepthStream.FrameWidth) -
                    (kinectButton.ActualWidth / 2.0));
                var handY = (int)((point.Y * LayoutRoot.ActualHeight / this.Kinect.DepthStream.FrameHeight) -
                    (kinectButton.ActualHeight / 2.0));
                Canvas.SetLeft(kinectButton, handX);
                Canvas.SetTop(kinectButton, handY);

                if (manoSobreBoton (kinectButton, buttons)) kinectButton.Hovering();
                else kinectButton.Release();
                if (hand.JointType == JointType.HandRight)
                {
                    kinectButton.ImageSource = "/WpfApplication1;component/Images/myhand.png";
                    kinectButton.ActiveImageSource = "/RVI_Education;component/Images/myhand.png";
                }
                else
                {
                    kinectButton.ImageSource = "/WpfApplication1;component/Images/myhand.png";
                    kinectButton.ActiveImageSource = "/WpfApplication1;component/Images/myhand.png";
                }
            }
        }
        private void BACKHOME_Click(object sender, RoutedEventArgs e)
        {
            UnregisterEvents();
            (Application.Current.MainWindow.FindName("_mainFrame") as Frame).Source = new Uri("MainMenu.xaml", UriKind.Relative);
        } 
  
    }
}
