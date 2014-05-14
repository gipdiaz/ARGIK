using System.IO;
using System.Windows;
using Kinect.Toolbox;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace GesturesViewer
{
    partial class MainWindow
    {
        String archivoPostura = Path.Combine(Environment.CurrentDirectory, @"data\abc.save");
        void LoadLetterTPostureDetector()
        {
           
            using (Stream recordStream = File.Open(archivoPostura, FileMode.OpenOrCreate))
            {
                
               // String nombrePosture = Console.ReadLine();
                templatePostureDetector = new TemplatedPostureDetector("ok", recordStream);
                templatePostureDetector.LearningMachine.Persist(recordStream);
                templatePostureDetector.PostureDetected += templatePostureDetector_PostureDetected;
            }
        }
        

        void ClosePostureDetector()
        {
            if (templatePostureDetector == null)
                return;

            using (Stream recordStream = File.Create(letterT_KBPath))
            {
                templatePostureDetector.SaveState(recordStream);
            }
            templatePostureDetector.PostureDetected -= templatePostureDetector_PostureDetected;
        }

        void templatePostureDetector_PostureDetected(string posture)
        {
            MessageBox.Show("Give me a......." + posture);
        }

        private void recordT_Click(object sender, RoutedEventArgs e)
        {
            
            recordNextFrameForPosture = true;

        }
    }
}
