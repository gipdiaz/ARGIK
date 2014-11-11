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
    partial class Paciente
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
        /// Activa la grabacion del gesto mediante el boton de la GUI
        /// </summary>
        public void cargarGesto()
        {
            List<string> lista = new List<string>();
            if (diccionarioPaciente.TryGetValue("Gestos", out lista))
            {
                if (lista.Count != 0)
                {
                    for (int i = 0; i < (lista.Count); i++)
                    {
                        System.Console.WriteLine(lista[i] + "- Lista");
                    }

                    //Guardar nombre de gesto, repeticiones y articulaciones
                    nombre_gesto = lista[0];
                    repeticion_gesto = Convert.ToInt32(lista[1]);
                    articulacion_gesto = lista[2];
                    sesion_gesto = lista[3];
                    System.Console.WriteLine(articulacion_gesto);
                    //Mostrar repeticiones

                    repeticionesDisplay.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    repeticionesDisplay.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    repeticionesDisplay.FontSize = 75;
                    repeticionesDisplay.Margin = new Thickness(30, 0, 0, 0);
                    repeticionesDisplay.Foreground = new SolidColorBrush(Colors.Red);

                    //lista.RemoveRange(0, 4);
                    //diccionarioPaciente.Remove("Gestos");
                    //diccionarioPaciente.Add("Gestos", lista);

                    //Comenzar a detectar el gesto
                    Stream recordStream = new FileStream(nombre_gesto, FileMode.Open);
                    reconocedorGesto = new TemplatedGestureDetector(nombre_gesto, recordStream);
                    reconocedorGesto.OnGestureDetected += OnGestureDetected;
                    repeticionesDisplay.Text = repeticion_gesto.ToString();
                    MouseController.Current.ClickGestureDetector = reconocedorGesto;
                    //gesturesCanvas.Children.Clear();
                    reconocedorGesto.DisplayCanvas = gesturesCanvas;
                }
                else
                    repeticionesDisplay.Text = "¡BIEN HECHO!";
            }
        }

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