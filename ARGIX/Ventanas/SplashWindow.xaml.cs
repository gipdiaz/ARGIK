using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Threading;
using System.Windows.Threading;

namespace ARGIK
{
    /// <summary>
    /// Interaction logic for splash.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        Thread loadingThread;
        Storyboard Showboard;
        Storyboard Hideboard;
        private delegate void ShowDelegate(string txt);
        private delegate void HideDelegate();
        ShowDelegate showDelegate;
        HideDelegate hideDelegate;
        private MediaPlayer mediaPlayer = new MediaPlayer();

        /// <summary>
        /// Initializes a new instance of the <see cref="SplashWindow"/> class.
        /// </summary>
        public SplashWindow()
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/inicio.wma", UriKind.Relative));
            mediaPlayer.Play();
            InitializeComponent();
            showDelegate = new ShowDelegate(this.showText);
            hideDelegate = new HideDelegate(this.hideText);
            Showboard = this.Resources["showStoryBoard"] as Storyboard;
            Hideboard = this.Resources["HideStoryBoard"] as Storyboard;
        
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadingThread = new Thread(load);
            loadingThread.Start();
        }
        private void load()
        {
            Thread.Sleep(1250);
            this.Dispatcher.Invoke(showDelegate, "Cargando Datos...");
            Thread.Sleep(1250);
            //load data 
            this.Dispatcher.Invoke(hideDelegate);

            Thread.Sleep(1250);
            this.Dispatcher.Invoke(showDelegate, "Inicializando Kinect...");
            Thread.Sleep(1250);
            //load data
            this.Dispatcher.Invoke(hideDelegate);
            
            //close the window
            Thread.Sleep(2000);
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
(Action)delegate() { Close(); });
        }
        private void showText(string txt)
        {
            txtLoading.Text = txt;
            BeginStoryboard(Showboard);
        }
        private void hideText()
        {
            BeginStoryboard(Hideboard);
        }

    }
}

