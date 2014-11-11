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
       
        int nroGesto = 0;
        string nombreSesion;
        public bool grabando = false;
        
        /// <summary>
        /// Se inicializa el detector de gestos con un Stream default
        /// </summary>
        public void CargarDetectorGestos()
        {
            using (Stream recordStream = new MemoryStream())
            {
                reconocedorGesto = new TemplatedGestureDetector("Gesto", recordStream);
                reconocedorGesto.DisplayCanvas = gesturesCanvas;
                MouseController.Current.ClickGestureDetector = reconocedorGesto;
            }
        }

        /// <summary>
        /// Se activa la grabacion del gesto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void grabarGesto_Click(object sender, RoutedEventArgs e)
        {
            grabarListaGestos();
        }

        /// <summary>
        /// Grabars the lista gestos.
        /// </summary>
        public void grabarListaGestos()
        {
            if (reconocedorGesto.IsRecordingPath)
            {
                StopRecord();
                reconocedorGesto.EndRecordTemplate();
                gesturesCanvas.Children.Clear();
                armarListaGestos();
            }
            else
            {
                CargarDetectorGestos();
                reconocedorGesto.StartRecordTemplate();
                grabando = true;
                nroGesto = nroGesto + 1;
                nombreSesion = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "sesion"+ nroGesto +".replay");
                DirectRecord(nombreSesion);
                repeticionesDisplay.Text = "¡Grabando!";
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
            lista.Add(reconocedorGesto.LearningMachine.repeticion);
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

        /// <summary>
        /// Se activa la deteccion del gesto seleccionado
        /// </summary>
        /// <param name="sender">La fuente del evento</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instancia que contiene los datos del evento.</param>
        //public void deteccionGesto_Click(object sender, RoutedEventArgs e)
        //{
        //    if (botonDetectarGesto.Content.ToString() == "Detectar Gesto")
        //    {
           
        //        OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Gestos files|*.save" };
        //        if (openFileDialog.ShowDialog() == true)
        //        {
        //            Stream recordStream = new FileStream(openFileDialog.FileName, FileMode.Open);
        //            reconocedorGesto = new TemplatedGestureDetector(openFileDialog.FileName, recordStream);
                    
        //            reconocedorGesto.OnGestureDetected += OnGestureDetected;

        //            MouseController.Current.ClickGestureDetector = reconocedorGesto;

        //            botonDetectarGesto.Content = "Pausar Detección";
        //            reconocedorGesto.DisplayCanvas = gesturesCanvas;              
        //        }

        //        //Limpiar puntos cuando cierra el cuadro de dialogo
        //        gesturesCanvas.Children.Clear();
        //    }
        //    else
        //    {
        //        botonDetectarGesto.Content = "Detectar Gesto";
        //        reconocedorGesto.OnGestureDetected -= OnGestureDetected;
        //    }
        //}
        }
}