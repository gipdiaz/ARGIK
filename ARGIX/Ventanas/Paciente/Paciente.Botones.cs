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
    partial class Paciente
    {
        /// <summary>
        /// Inicia la sesion del paciente cargando los gestos a realizar (boton rojo RA)
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instancia que contiene los datos del evento.</param>
        public void botonReproducirSesion_Clicked(object sender, RoutedEventArgs e)
        {
            if (this.botonReproducirSesion.IsChecked)
            {
                //Desearilzar el diccionario
                mensajePantalla.Text = "";
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextReader textReader = new StreamReader(@"Gaston Diaz.xml");
                diccionario = (SerializableDictionary<string, List<string>>)serializer.Deserialize(textReader);
                cargarReplay();
                sesionIniciada = true;
                botonRepetirGesto.Visibility = Visibility.Visible;
            }
            else
            {
                diccionario = null;
                mensajePantalla.Text = "Sesión Finalizada";
                retirarReconocedorGesto();
                sesionIniciada = false;
                botonRepetirGesto.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Hace lo mismo que el metodo click, pero es llamado por voz
        /// </summary>
        public void ReproducirSesion()
        {
            if (this.botonReproducirSesion.IsChecked == false)
            {
                this.botonReproducirSesion.IsChecked = true;
                sesionIniciada = true;
                mensajePantalla.Text = "";
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextReader textReader = new StreamReader(@"Gaston Diaz.xml");
                diccionario = (SerializableDictionary<string, List<string>>)serializer.Deserialize(textReader);
                cargarReplay();
            }
        }

        public void detenerSesion()
        {
            if (this.botonReproducirSesion.IsChecked)
            {
                this.botonReproducirSesion.IsChecked = false;
                sesionIniciada = false;
                diccionario = null;
                mensajePantalla.Text = "Sesión Finalizada";
                retirarReconocedorGesto();
            }
        }

        /// <summary>
        /// Handles the Clicked event of the botonRepetirGesto control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void botonRepetirGesto_Clicked(object sender, RoutedEventArgs e)
        {
            cargarReplay();
        }

        /// <summary>
        /// Repetirs the replay.
        /// </summary>
        private void RepetirGesto()
        {
            cargarReplay();
        }

        /// <summary>
        /// Handles the Clicked event of the botonNegroPaciente control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void botonAyudaPaciente_Clicked(object sender, RoutedEventArgs e)
        {
            if (ayudaHabilitada == false)
                ayudaHabilitada = true;
            else
                ayudaHabilitada = false;
            habilitarAyudas();
        }

        private void botonVerdePaciente_Clicked(object sender, RoutedEventArgs e)
        {
            
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

        /// <summary>
        /// Inicializa las ayudas de la ventana MEDICO
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// 
        public void CargarAyudas()
        {
            ayudaHabilitada = false;

            ayudaIniciarSesion.Text = "Iniciar la sesion para poder\nrealizar los movimientos";
            ayudaRepetir.Text = "Repetir el gesto para\nvisualizar el movimiento";
            ayudaAyuda.Text = "Activar el modo ayuda de la\naplicación";
            ayudaSalir.Text = "Volver al menú principal";
            habilitarAyudas();
        }
    }
}