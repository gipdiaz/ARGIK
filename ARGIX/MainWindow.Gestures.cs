using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Threading;



namespace GesturesViewer
{
    /// <summary>
    /// Esta parte de la clase se encarga del manejo de la deteccion/grabacion de gestos. 
    /// </summary>
    
    partial class MainWindow
    {
        TextBlock nombreGesto = new TextBlock();
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        /// <summary>
        /// Se inicializa el detector de gestos con un Stream default
        /// </summary>
        public void CargarDetectorGestos()
        {

            //Establece en 3 segundos el tiempo para mostrar el nombre del gesto en pantalla. 
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
                

            using (Stream recordStream = new MemoryStream())
            {

                reconocedorGesto = new TemplatedGestureDetector("Gesto", recordStream);
                reconocedorGesto.DisplayCanvas = gesturesCanvas;
                MouseController.Current.ClickGestureDetector = reconocedorGesto;
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            LayoutRoot.Children.Remove(nombreGesto);
        }

        /// <summary>
        /// Se activa la grabacion del gesto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void grabarGesto_Click(object sender, RoutedEventArgs e)
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
        /// Se activa la deteccion del gesto seleccionado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void deteccionGesto_Click(object sender, RoutedEventArgs e)
        {
            if (botonDetectarGesto.Content.ToString() == "Detectar Gesto")
            {
                

                OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Gestos files|*.save" };
                if (openFileDialog.ShowDialog() == true)
                {
                    Stream recordStream = new FileStream(openFileDialog.FileName, FileMode.Open);
                    reconocedorGesto = new TemplatedGestureDetector(openFileDialog.FileName, recordStream);
                    reconocedorGesto.OnGestureDetected += OnGestureDetected;
                    MouseController.Current.ClickGestureDetector = reconocedorGesto;

                    botonDetectarGesto.Content = "Pausar Detección";
                    reconocedorGesto.DisplayCanvas = gesturesCanvas;              
                }
                //Limpiar puntos cuando cierra el cuadro de dialogo
                gesturesCanvas.Children.Clear();
              
            }
            else
            {
                botonDetectarGesto.Content = "Detectar Gesto";
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
            }
        }

        /// <summary>
        /// Si se detecta el gesto seleccionado se muestra cuando se lo realiza correctamente
        /// </summary>
        /// <param name="gesture"></param>
        public void OnGestureDetected(string gesture)


        {
            //Obtener nombre del gesto sin extension
            gesture = Path.GetFileNameWithoutExtension(gesture);
            
            
            nombreGesto.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            nombreGesto.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            nombreGesto.FontSize = 75;
            nombreGesto.Margin = new Thickness(30, 0, 0, 0);
            nombreGesto.Foreground = new SolidColorBrush(Colors.Red);
            nombreGesto.Text = gesture;
            nombreGesto.Visibility = System.Windows.Visibility.Visible;
            LayoutRoot.Children.Add (nombreGesto);
            
           
            dispatcherTimer.Start(); 

            

            int pos = detectedGestures.Items.Add(string.Format("{0} ---- {1}", gesture, DateTime.Now));
            object item = detectedGestures.Items[pos];
            detectedGestures.ScrollIntoView(item);
            detectedGestures.SelectedItem = item;

        }

        /// <summary>
        /// Limpia los recursos utilizados para la deteccion/grabacion de gestos
        /// </summary>
        public void CerrarDetectorGestos()
        {
            if (reconocedorGesto == null)
                return;
            else
            {
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                
            }
        }
    }
}