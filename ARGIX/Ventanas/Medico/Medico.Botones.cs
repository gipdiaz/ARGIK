using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Threading;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ARGIK
{
    // Esta parte de la clase se encarga de manejar los gestos
    partial class Medico
    {
        /// <summary>
        /// Boton Rojo de RA, genera el XML con la lista.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">La <see cref="RoutedEventArgs" /> instancia contiene los datos del evento.</param>
        public void botonGrabar_Clicked(object sender, RoutedEventArgs e)
        {
            if (this.botonGrabarSesion.IsChecked)
            {
                diccionario = new SerializableDictionary<string, List<string>>();
                List<string> medico = new List<string>();
                medico.Add("German Leschevich");
                List<string> paciente = new List<string>();
                paciente.Add("Gaston Diaz");
                List<string> precision = new List<string>();
                precision.Add("Media");
                List<string> gestos = new List<string>();

                diccionario.Add("Medico", medico);
                diccionario.Add("Paciente", paciente);
                diccionario.Add("Precision", precision);
                diccionario.Add("Gestos", gestos);
            }
            else
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextWriter textWriter = new StreamWriter(@"Gaston Diaz.xml");
                serializer.Serialize(textWriter, diccionario);
                textWriter.Close();
            }
        }

        /// <summary>
        /// Handles the Clicked event of the botonArticulacion control.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instancia que contiene los datos del evento.</param>
        public void botonSeleccionarArticulacion_Clicked(object sender, RoutedEventArgs e)
        {
            this.Clean();
            this.Visibility = Visibility.Collapsed;
            Articulaciones jointsNuevo = new Articulaciones(this);
            jointsNuevo.ShowDialog();
            this.Visibility = Visibility.Visible;
            this.Reanudar();
        }

        private void botonAyuda_Clicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles the Click event of the ATRAS control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ATRAS_Click(object sender, RoutedEventArgs e)
        {
            MenuPrincipal menuPrincipal = new MenuPrincipal(modoSentado);
            this.Clean();
            this.Close();
            menuPrincipal.Show();
        }
    }
}