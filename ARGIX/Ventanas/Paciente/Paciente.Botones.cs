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
        public void botonGesto_Clicked(object sender, RoutedEventArgs e)
        {
            //Desearilzar el diccionario
            grabando = false;
            repeticionesDisplay.Text = "";
            XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, List<string>>));
            TextReader textReader = new StreamReader(@"Gaston Diaz.xml");
            diccionarioPaciente = (SerializableDictionary<string, List<string>>)serializer.Deserialize(textReader);
            cargarReplay();
            //botonDetectarGesto.Content = "Pausar Detección azul";
            reconocedorGesto.DisplayCanvas = gesturesCanvas;
            //Limpiar puntos cuando cierra el cuadro de dialogo
            // gesturesCanvas.Children.Clear();

            //else
            //{
            //    botonDetectarGesto.Content = "Detectar Gesto";
            //    reconocedorGesto.OnGestureDetected -= OnGestureDetected;
            //}
        }

        private void botonRepetirGesto_Clicked(object sender, RoutedEventArgs e)
        {
            repitiendo_gesto = true;
            cargarReplay();
        }

        private void botonNegroPaciente_Clicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void botonAzulPaciente_Clicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}