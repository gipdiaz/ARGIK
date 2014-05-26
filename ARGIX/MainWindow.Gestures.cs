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
        //String archivoGesto = Path.Combine(Environment.CurrentDirectory, @"data\L.save");
        //bool primervez = true;


        void LoadCircleGestureDetector()
        {
            
            using (Stream recordStream = File.Open(circleKBPath, FileMode.OpenOrCreate))
            {
                               
                circleGestureRecognizer = new TemplatedGestureDetector("L", recordStream);
                circleGestureRecognizer.DisplayCanvas = gesturesCanvas;
                //circleGestureRecognizer.LearningMachine.Persist(recordStream);
                circleGestureRecognizer.OnGestureDetected += OnGestureDetected;

                MouseController.Current.ClickGestureDetector = circleGestureRecognizer;
            }
        }

        private void recordGesture_Click(object sender, RoutedEventArgs e)
        {

            //if (primervez) {
              //  SaveFileDialog saveFileDialog = new SaveFileDialog { Title = "Elija nombre de gesto", Filter = "Archivos Gesto|*.gesto" };
                //archivoGesto = saveFileDialog.FileName;
            //}
                if (circleGestureRecognizer.IsRecordingPath)
                {
                    

                        circleGestureRecognizer.EndRecordTemplate();
                        recordGesture.Content = "Grabar Gesto";
                        return;
                    
                }
                circleGestureRecognizer.StartRecordTemplate();
                recordGesture.Content = "Pausar Grabacion";
            
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

            using (Stream recordStream = File.Create(circleKBPath))
            {
                circleGestureRecognizer.SaveState(recordStream);
            }
            circleGestureRecognizer.OnGestureDetected -= OnGestureDetected;
        }
    }
}
