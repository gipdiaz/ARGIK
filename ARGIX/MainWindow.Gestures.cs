using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;
using Microsoft.Win32;


namespace GesturesViewer
{
    partial class MainWindow
    {
        String archivoGesto = "";
               


        void LoadCircleGestureDetector()
        {
            
            OpenFileDialog openFileDialog = new OpenFileDialog {Title = "Select filename", Filter = "Save files|*.save"};
            if (openFileDialog.ShowDialog() == true)
            {
                using (Stream recordStream = File.Open(openFileDialog.FileName, FileMode.OpenOrCreate))
                {
                    archivoGesto = openFileDialog.FileName;
                    circleGestureRecognizer = new TemplatedGestureDetector(archivoGesto, recordStream);
                    circleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                    
                    circleGestureRecognizer.OnGestureDetected += OnGestureDetected;
                    MouseController.Current.ClickGestureDetector = circleGestureRecognizer;
                }
            }
        }

        //Grabar el gesto
        private void recordGesture_Click(object sender, RoutedEventArgs e)
        {
            

                if (circleGestureRecognizer.IsRecordingPath)
                {
                    circleGestureRecognizer.EndRecordTemplate();
                    recordGesture.Content = "Grabar Gesto";
                    return;
                }

                circleGestureRecognizer.StartRecordTemplate();
                recordGesture.Content = "Pausar Grabacion";
            }
             
    //Cargar el gesto y habilitar reconocimiento
        
        private void recordT_Click(object sender, RoutedEventArgs e)
        {
     
            OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Save files|*.save" };
            if (openFileDialog.ShowDialog() == true)
            {
                using (Stream recordStream = File.Open(openFileDialog.FileName, FileMode.OpenOrCreate))
                {
                    archivoGesto = openFileDialog.FileName;
                    circleGestureRecognizer = new TemplatedGestureDetector(archivoGesto, recordStream);
                    circleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                    circleGestureRecognizer.OnGestureDetected += OnGestureDetected;

                    MouseController.Current.ClickGestureDetector = circleGestureRecognizer;
                    detectando = true;
                }
            }
        }

        void OnGestureDetected(string gesture)
        {
            int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));

            detectedGestures.SelectedIndex = pos;
        }

        void CloseGestureDetector()
        {
            if (circleGestureRecognizer == null)
                return;
            else
            {
                using (Stream recordStream = File.Create(circleGestureRecognizer.LearningMachine.gestoNuevo))
                {
                    circleGestureRecognizer.SaveState(recordStream);
                }
                circleGestureRecognizer.OnGestureDetected -= OnGestureDetected;
            }
        }
    }
}
