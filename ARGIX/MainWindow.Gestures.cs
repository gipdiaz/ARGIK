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


namespace GesturesViewer
{
    // Esta parte de la clase se encarga de manejar los gestos
    partial class MainWindow
    {
       
        int nroGesto = 0;
        string nombreSesion;
        bool grabando = false;
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

        public void grabarListaGestos()
        {
            
            if (reconocedorGesto.IsRecordingPath)
            {
                StopRecord();
                reconocedorGesto.EndRecordTemplate();
                gesturesCanvas.Children.Clear();
                //System.IO.File.Move(nombreSesion, System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "sesion_" + Path.GetFileNameWithoutExtension(reconocedorGesto.LearningMachine.gestoNuevo)+ ".replay"));
                armarListaGestos();
                
            }
            else
            {
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                CargarDetectorGestos();
                reconocedorGesto.StartRecordTemplate();
                grabando = true;
                //botonGrabarGesto.Content = "Pausar Grabacion";
                nroGesto = nroGesto + 1;
                nombreSesion = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "sesion"+ nroGesto +".replay");
                DirectRecord(nombreSesion);
                repeticionesDisplay.Text = "¡Grabando!";
            }
        }

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

            //botonGrabarGesto.Content = "Grabar Gesto";
            
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
                //botonGrabarGestoViejo.Content = "Grabar Gesto Viejo";

            }
            else
            {
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                CargarDetectorGestos();
                reconocedorGesto.StartRecordTemplate();
                //botonGrabarGestoViejo.Content = "Pausar Grabacion Viejo";
            }
        }

        /// <summary>
        /// Se activa la deteccion del gesto seleccionado
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Si se detecta el gesto seleccionado se muestra cuando se lo realiza correctamente
        /// </summary>
        /// <param name="gesture">The gesture.</param>
        public void OnGestureDetected(string gesture)
        {

            //Obtener nombre del gesto sin extension
            //gesture = Path.GetFileNameWithoutExtension(gesture);

                repitiendo_gesto = false;
              
                repeticion_gesto = repeticion_gesto - 1;
                repeticionesDisplay.Text = repeticion_gesto.ToString();

                //int pos = detectedGestures.Items.Add(string.Format("{0} ---- {1}", gesture, DateTime.Now));
                //object item = detectedGestures.Items[pos];
                //detectedGestures.ScrollIntoView(item);
                //detectedGestures.SelectedItem = item;

                if (repeticion_gesto == 0)
                {
                    List<string> lista = new List<string>();
                    if (diccionarioPaciente.TryGetValue("Gestos", out lista))
                    {
                        if (lista.Count != 0)
                        {
                            lista.RemoveRange(0, 4);
                            diccionarioPaciente.Remove("Gestos");
                            diccionarioPaciente.Add("Gestos", lista);
                        }
                    }
                    reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                    cargarReplay();
                }
            

        }

        /// <summary>
        /// Limpia los recursos utilizados para la deteccion/grabacion de gestos
        /// </summary>
        public void CerrarDetectorGestos()
        {
            if (reconocedorGesto == null)
                return;
            else
            {
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                
            }
        }
    }
}