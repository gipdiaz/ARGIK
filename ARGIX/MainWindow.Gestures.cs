using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;
using Microsoft.Win32;


namespace GesturesViewer
{
    /// <summary>
    /// Esta parte de la clase se encarga del manejo de la deteccion/grabacion de gestos. 
    /// </summary>
    
    partial class MainWindow
    {
       
        /// <summary>
        /// Se inicializa el detector de gestos con un Stream default
        /// </summary>
        public void CargarDetectorGestos()
        {

            using (Stream recordStream = new MemoryStream())
            {

                reconocedorGesto = new TemplatedGestureDetector("Gesto", recordStream);
                reconocedorGesto.DisplayCanvas = gesturesCanvas;
                //reconocedorGesto.OnGestureDetected += OnGestureDetected;
                MouseController.Current.ClickGestureDetector = reconocedorGesto;
                //recordStream.Close();
            }

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
                    reconocedorGesto.DisplayCanvas = gesturesCanvas;
                    reconocedorGesto.OnGestureDetected += OnGestureDetected;

                    MouseController.Current.ClickGestureDetector = reconocedorGesto;
                    
                    botonDetectarGesto.Content = "Pausar Detección";
                }

            }
            else
            {
                botonDetectarGesto.Content = "Detectar Gesto";
            }
        }

        /// <summary>
        /// Si se detecta el gesto seleccionado se muestra cuando se lo realiza correctamente
        /// </summary>
        /// <param name="gesture"></param>
        public void OnGestureDetected(string gesture)


        {

            gesture = Path.GetFileNameWithoutExtension(gesture);
            int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));

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
                using (Stream recordStream = File.Create(reconocedorGesto.LearningMachine.gestoNuevo))
                {
                    reconocedorGesto.SaveState(recordStream);
                    recordStream.Close();
                }
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
            }
        }
    }
}