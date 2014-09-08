using System.IO;
using System.Windows;
using Kinect.Toolbox.Record;
using Microsoft.Win32;

namespace GesturesViewer
{
    // Esta parte de la clase se encarga de manejar la grabacion de la sesion
    partial class MainWindow
    {
        /// <summary>
        /// Permite ponerle un nombre al archivo de la sesion que se graba.
        /// Graba la sesion
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void recordOption_Click(object sender, RoutedEventArgs e)
        {
            if (recorder != null)
            {
                StopRecord();
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog { Title = "Select filename", Filter = "Replay files|*.replay" };
            if (saveFileDialog.ShowDialog() == true)
            {
                DirectRecord(saveFileDialog.FileName);
            }
        }

        /// <summary>
        /// Graba la sesion y la guarda en el archivo con el nombre correspondiente.
        /// </summary>
        /// <param name="targetFileName">Name of the target file.</param>
        public void DirectRecord(string targetFileName)
        {
            Stream recordStream = File.Create(targetFileName);
            recorder = new KinectRecorder(KinectRecordOptions.Skeletons | KinectRecordOptions.Color, recordStream);
            recordOption.Content = "Parar Grabación";
        }

        /// <summary>
        /// Detiene la grabacion
        /// </summary>
        public void StopRecord()
        {
            if (recorder != null)
            {
                recorder.Stop();
                recorder = null;
                recordOption.Content = "Grabar Sesión";
                return;
            }
        }
    }
}
