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

            using (Stream recordStream = new MemoryStream())
            {

                circleGestureRecognizer = new TemplatedGestureDetector("Gesto", recordStream);
                circleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                //circleGestureRecognizer.OnGestureDetected += OnGestureDetected;

                MouseController.Current.ClickGestureDetector = circleGestureRecognizer;
                //recordStream.Close();
            }

        }

        //Grabar el gesto
        private void recordGesture_Click(object sender, RoutedEventArgs e)
        {


            if (circleGestureRecognizer.IsRecordingPath)
            {
                circleGestureRecognizer.EndRecordTemplate();
                recordGesture.Content = "Grabar Gesto";

            }
            else
            {
                circleGestureRecognizer.OnGestureDetected -= OnGestureDetected;
                LoadCircleGestureDetector();
                circleGestureRecognizer.StartRecordTemplate();
                recordGesture.Content = "Pausar Grabacion";
            }
        }

        //Cargar el gesto y habilitar reconocimiento

        private void recordT_Click(object sender, RoutedEventArgs e)
        {
            if (recordT.Content.ToString() == "Detectar Gesto")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Gestos files|*.save" };

                if (openFileDialog.ShowDialog() == true)
                {
                    Stream recordStream = new FileStream(openFileDialog.FileName, FileMode.Open);


                    circleGestureRecognizer = new TemplatedGestureDetector(openFileDialog.FileName, recordStream);
                    circleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                    circleGestureRecognizer.OnGestureDetected += OnGestureDetected;

                    MouseController.Current.ClickGestureDetector = circleGestureRecognizer;
                    detectando = true;
                    recordT.Content = "Pausar Detección";
                }

            }
            else
            {
                detectando = false;

                recordT.Content = "Detectar Gesto";
            }
        }


        void OnGestureDetected(string gesture)


        {
            //gesture.ToString;


            
            int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));

            object item = detectedGestures.Items[pos];
            detectedGestures.ScrollIntoView(item);
            detectedGestures.SelectedItem = item;

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
                    recordStream.Close();
                }
                circleGestureRecognizer.OnGestureDetected -= OnGestureDetected;
            }
        }
    }
}