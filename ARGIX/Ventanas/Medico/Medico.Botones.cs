using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Input;
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
                // Sonido
                mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
                mediaPlayer.Play();

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

                sesionIniciada = true;
                botonSeleccionarArticulacion.Visibility = Visibility.Visible;
            }
            else
            {
                // Sonido
                mediaPlayer.Open(new Uri(@"../../Media/button-22.mp3", UriKind.Relative));
                mediaPlayer.Play();

                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextWriter textWriter = new StreamWriter(@"Gaston Diaz.xml");
                serializer.Serialize(textWriter, diccionario);
                textWriter.Close();
                sesionIniciada = false;
                botonSeleccionarArticulacion.Visibility = Visibility.Hidden;
            }
            habilitarAyudas();
        }

        /// <summary>
        /// Handles the Clicked event of the botonArticulacion control.
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instancia que contiene los datos del evento.</param>
        public void botonSeleccionarArticulacion_Clicked(object sender, RoutedEventArgs e)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
            mediaPlayer.Play();

            this.Clean();
            Articulaciones jointsNuevo = new Articulaciones(this);
            jointsNuevo.ShowDialog();
            this.Reanudar();
        }

        private void botonAyuda_Clicked(object sender, RoutedEventArgs e)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
            mediaPlayer.Play();

            if (ayudaHabilitada == false)
                ayudaHabilitada = true;
            else
                ayudaHabilitada = false;
            habilitarAyudas();
        }

        /// <summary>
        /// Handles the Click event of the ATRAS control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ATRAS_Click(object sender, RoutedEventArgs e)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-21.mp3", UriKind.Relative));
            mediaPlayer.Play();

            this.Clean();
            MenuPrincipal menuPrincipal = new MenuPrincipal(modoSentado);
            menuPrincipal.Show();
            this.Close();
        }

        /// <summary>
        /// Inicializa las ayudas de la ventana MEDICO
        /// </summary>
        public void CargarAyudas()
        {
            ayudaHabilitada = false;

            ayudaGrabarSesion.Text = "Iniciar la sesion para poder\ngrabar los movimientos";
            ayudaArticulaciones.Text = "Seleccionar la articulación que\nse utilizará para grabar el gesto";
            ayudaAyuda.Text = "Activar/Desactivar el modo \nayuda de la aplicación";
            ayudaSalir.Text = "Volver al menú principal";
            //ayudaVoz.Text = "COMANDOS DE VOZ \nGRABAR -> Iniciar la grabación del gesto\nDETENER -> Finalizar grabación del gesto\nINFO -> Activar/Desactivar Ayuda\nSALIR -> Volver al Menú Principal";
            habilitarAyudas();
        }
    }
}