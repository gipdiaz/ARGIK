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

namespace ARGIK
{
    // Esta parte de la clase se encarga de manejar los gestos
    partial class Medico
    {

        int nroGesto =0;
        string nombreSesion;
        public bool grabando = false;
        string nroRepeticiones;
        /// <summary>
        /// Se inicializa el detector de gestos con un Stream default
        /// </summary>
        public void CargarDetectorGestos()
        {
            using (Stream recordStream = new MemoryStream())
            {
                reconocedorGesto = new TemplatedGestureDetector("Gesto", recordStream);
                reconocedorGesto.DisplayCanvas = gesturesCanvas;
               
            }
        }

        /// <summary>
        /// Grabars the lista gestos.
        /// </summary>
        public void grabarListaGestos()
        {
            if (reconocedorGesto.IsRecordingPath)
            {
                mensajePantalla.Text = "";
                StopRecord();
                reconocedorGesto.EndRecordTemplate();
                gesturesCanvas.Children.Clear();
                botonAyuda.Visibility = Visibility.Visible;
                botonGrabarSesion.Visibility = Visibility.Visible;
                botonSeleccionarArticulacion.Visibility = Visibility.Visible;
                grabando = false;

                //Instanciar dialogo de repeticiones             

                RepeticionesDialog rep = new RepeticionesDialog();
                // Configure the dialog box
                 rep.ShowDialog();
                 nroRepeticiones = rep.repeticion;
                armarListaGestos();
            }
            else
            {
                mensajePantalla.Text = "¡Grabando!";
                CargarDetectorGestos();
                reconocedorGesto.StartRecordTemplate();
                grabando = true;
                botonAyuda.Visibility = Visibility.Hidden;
                botonGrabarSesion.Visibility = Visibility.Hidden;
                botonSeleccionarArticulacion.Visibility = Visibility.Hidden;
                nroGesto = nroGesto + 1;
                nombreSesion = System.IO.Path.Combine(Environment.CurrentDirectory, "sesion" + nroGesto + ".replay");
                DirectRecord(nombreSesion);
            }
        }

        /// <summary>
        /// Arma la lista de gestos
        /// </summary>
        public void armarListaGestos()
        {
            List<string> lista = new List<string>();
            diccionario.TryGetValue("Gestos", out lista);
            lista.Add(reconocedorGesto.LearningMachine.gestoNuevo);
            lista.Add(nroRepeticiones);
            lista.Add(articulacion_gesto);
            lista.Add(nombreSesion);
            diccionario.Remove("Gestos");
            diccionario.Add("Gestos", lista);

        }

        /// <summary>
        /// Se activa la grabacion del gesto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void grabarGestoViejo_Click(object sender, RoutedEventArgs e)
        {
            if (reconocedorGesto.IsRecordingPath)
            {
                reconocedorGesto.EndRecordTemplate();
            }
            else
            {
                CargarDetectorGestos();
                reconocedorGesto.StartRecordTemplate();
            }
        }
    }
}