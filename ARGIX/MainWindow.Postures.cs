using System.IO;
using System.Windows;
using Kinect.Toolbox;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace ARGIX
{
    partial class MainWindow
    {
        String archivoPostura = Path.Combine(Environment.CurrentDirectory, @"..\..\gestos_posturas\t.save");

        /// <summary>
        /// Cargars the detector posturas.
        /// </summary>
        void CargarDetectorPosturas()
        {           
            using (Stream recordStream = File.Open(archivoPostura, FileMode.OpenOrCreate))
            {       
                // String nombrePosture = Console.ReadLine();
                //templatePostureDetector = new TemplatedPostureDetector("ok", recordStream);
                //templatePostureDetector.LearningMachine.Persist(recordStream);
                algorithmicPostureRecognizer.PostureDetected += algorithmicPostureRecognizer_PostureDetected;
                //templatePostureDetector.PostureDetected += templatePostureDetector_PostureDetected;
            }
        }

        /// <summary>
        /// Handles the Click event of the grabarPostura control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void grabarPostura_Click(object sender, RoutedEventArgs e)
        {
            recordNextFrameForPosture = true;
        }

        /// <summary>
        /// Closes the posture detector.
        /// </summary>
        void ClosePostureDetector()
        {
            if (templatePostureDetector == null)
                return;

            using (Stream recordStream = File.Create(letterT_KBPath))
            {
                templatePostureDetector.SaveState(recordStream);
            }
            templatePostureDetector.PostureDetected -= algorithmicPostureRecognizer_PostureDetected;
        }

        void algorithmicPostureRecognizer_PostureDetected(string posture)
        {
            //MessageBox.Show("Give me a......." + posture);

            // VER QUE CONCHA HACER CON ESTO
            //posture = Path.GetFileNameWithoutExtension(posture);
            //int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", posture, DateTime.Now));
            //object item = detectedGestures.Items[pos];
            //detectedGestures.ScrollIntoView(item);
            //detectedGestures.SelectedItem = item;
        }       
    }
}
