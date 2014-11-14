using System.IO;
using System.Windows;
using Kinect.Toolbox;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace ARGIK
{
    partial class Medico
    {

        /// <summary>
        /// Cargar el detector de posturas
        /// </summary>
        public void CargarDetectorPosturas()
        {           
            algorithmicPostureRecognizer.PostureDetected += algorithmicPostureRecognizer_PostureDetected;
        }

        /// <summary>
        /// Cerrar el detector de posturas
        /// </summary>
        public void CerrarDetectorPosturas()
        {
            algorithmicPostureRecognizer.PostureDetected -= algorithmicPostureRecognizer_PostureDetected;
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
