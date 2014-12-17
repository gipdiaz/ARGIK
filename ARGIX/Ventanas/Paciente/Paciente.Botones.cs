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
                // Sonido
                mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
                mediaPlayer.Play();

                //Desearilzar el diccionario
                mensajePantalla.Text = "";
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextReader textReader = new StreamReader(@"Gaston Diaz.xml");
                diccionario = (SerializableDictionary<string, List<string>>)serializer.Deserialize(textReader);
                cargarReplay();
                sesionIniciada = true;
                botonRepetirGesto.Visibility = Visibility.Visible;
                habilitarAyudas();
            }
            else
            {
                // Sonido
                mediaPlayer.Open(new Uri(@"../../Media/button-22.mp3", UriKind.Relative));
                mediaPlayer.Play();

                diccionario = null;
                mensajePantalla.Text = "Sesión Finalizada";
                retirarReconocedorGesto();
                sesionIniciada = false;
                botonRepetirGesto.Visibility = Visibility.Hidden;
                habilitarAyudas();
            }
        }

        /// <summary>
        /// Hace lo mismo que el metodo click, pero es llamado por voz
        /// </summary>
        public void ReproducirSesion()
        {
            if (this.botonReproducirSesion.IsChecked == false)
            {
                // Sonido
                mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
                mediaPlayer.Play();

                this.botonReproducirSesion.IsChecked = true;
                sesionIniciada = true;
                mensajePantalla.Text = "";
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
                TextReader textReader = new StreamReader(@"Gaston Diaz.xml");
                diccionario = (SerializableDictionary<string, List<string>>)serializer.Deserialize(textReader);
                cargarReplay();
                botonRepetirGesto.Visibility = Visibility.Visible;
                habilitarAyudas();


            }


        }

        /// <summary>
        /// Deteners the sesion.
        /// </summary>
        public void detenerSesion()
        {
            if (this.botonReproducirSesion.IsChecked)
            {
                // Sonido
                mediaPlayer.Open(new Uri(@"../../Media/button-22.mp3", UriKind.Relative));
                mediaPlayer.Play();

                this.botonReproducirSesion.IsChecked = false;
                sesionIniciada = false;
                diccionario = null;
                mensajePantalla.Text = "Sesión Finalizada";
                retirarReconocedorGesto();
                botonRepetirGesto.Visibility = Visibility.Hidden;
                habilitarAyudas();

                replay.SkeletonFrameReady -= replay_SkeletonFrameReady;
                replay.ColorImageFrameReady -= replay_ColorImageFrameReady;
                replay.Dispose();
                replay.Stop();
            }
        }

        /// <summary>
        /// Handles the Clicked event of the botonRepetirGesto control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void botonRepetirGesto_Clicked(object sender, RoutedEventArgs e)
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
            mediaPlayer.Play();

            cargarReplay();
        }

        /// <summary>
        /// Repetirs the replay.
        /// </summary>
        private void RepetirGesto()
        {
            // Sonido
            mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
            mediaPlayer.Play();

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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// 
        public void CargarAyudas()
        {
            ayudaHabilitada = false;

            ayudaIniciarSesion.Text = "Iniciar/Finalizar la sesion";
            ayudaRepetir.Text = "Repetir el gesto para\nvisualizar el movimiento";
            ayudaAyuda.Text = "Activar/Desactivar el modo\n ayuda de la aplicación";
            ayudaSalir.Text = "Volver al menú principal";
            //ayudaVoz.Text = "*************Comandos de voz*************\n\nREPRODUCIR -->Iniciar la sesion de gestos\nDETENER ---> Finalizar la sesión\nREPETIR ---> Repetir demostración del gesto\nINFO ---> Activar/Desactivar Ayuda\nSALIR ---> Volver al Menú Principal";
            habilitarAyudas();
        }
    }
}