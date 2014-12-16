using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Threading;
using System.Collections.Generic;

namespace ARGIK
{
    // Esta parte de la clase se encarga de manejar los gestos
    partial class Paciente
    {
        bool cargar_gesto;
        string nombre_gesto;
        int repeticion_gesto;
        string sesion_gesto;
        string articulacion_gesto;

        /// <summary>
        /// Se inicializa el detector de gestos con un Stream default
        /// </summary>
        public void CargarDetectorGestos()
        {
            using (Stream recordStream = new MemoryStream())
            {
                reconocedorGesto = new TemplatedGestureDetector("Gesto", recordStream);
            }
        }

        /// <summary>
        /// Activa la grabacion del gesto mediante el boton de la GUI
        /// </summary>
        public void cargarGesto()
        {
            List<string> lista = new List<string>();
            if (diccionario.TryGetValue("Gestos", out lista))
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


                    //Mostrar repeticiones
                    //mensajePantalla.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    //mensajePantalla.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    //mensajePantalla.FontSize = 75;
                    //mensajePantalla.Margin = new Thickness(30, 0, 0, 0);
                    mensajePantalla.Foreground = new SolidColorBrush(Colors.Red);

                    //Comenzar a detectar el gesto
                    Stream recordStream = new FileStream(nombre_gesto, FileMode.Open);
                    reconocedorGesto = new TemplatedGestureDetector(nombre_gesto, recordStream);
                    reconocedorGesto.OnGestureDetected += OnGestureDetected;
                    //mensajePantalla.FontSize = 20;
                    mensajePantalla.Text = repeticion_gesto.ToString();
                    gesturesCanvas.Children.Clear();
                    reconocedorGesto.DisplayCanvas = gesturesCanvas;
                }
                else
                {
                    mensajePantalla.Text = "¡BIEN HECHO!";
                    detenerSesion();
                    //botonRepetirGesto.Visibility = Visibility.Hidden;
                }
                cargar_gesto = false;
            }
        }

        /// <summary>
        /// Retirars the reconocedor gesto.
        /// </summary>
        public void retirarReconocedorGesto()
        {
            reconocedorGesto.OnGestureDetected -= OnGestureDetected;
            reconocedorGesto.DisplayCanvas = null;
        }

        /// <summary>
        /// Si se detecta el gesto seleccionado se muestra cuando se lo realiza correctamente
        /// </summary>
        /// <param name="gesture">The gesture.</param>
        public void OnGestureDetected(string gesture)
        {
            repeticion_gesto = repeticion_gesto - 1;
            mensajePantalla.Text = repeticion_gesto.ToString();

            if (repeticion_gesto == 0)
            {
                List<string> lista = new List<string>();
                if (diccionario.TryGetValue("Gestos", out lista))
                {
                    if (lista.Count != 0)
                    {
                        lista.RemoveRange(0, 4);
                        diccionario.Remove("Gestos");
                        diccionario.Add("Gestos", lista);
                    }
                }
                reconocedorGesto.OnGestureDetected -= OnGestureDetected;
                cargarReplay();
                cargar_gesto = true;
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

        /// <summary>
        /// Carga la repetición del gesto.
        /// </summary>
        public void cargarReplay()
        {
            mensajePantalla.Text = "";
            List<string> lista = new List<string>();
            if (diccionario.TryGetValue("Gestos", out lista))
            {
                if (lista.Count != 0)
                {

                    for (int i = 0; i < (lista.Count); i++)
                    {
                        System.Console.WriteLine(lista[i] + "- Lista");
                    }

                    //frenar la deteccion de gestos
                    reconocedorGesto.OnGestureDetected -= OnGestureDetected;

                    //Guardar nombre de gesto, repeticiones y articulaciones
                    nombre_gesto = lista[0];
                    articulacion_gesto = lista[2];
                    sesion_gesto = lista[3];

                    //Frenar cualquier repeticion que se este ejecutando
                    if (replay != null)
                    {
                        replay.SkeletonFrameReady -= replay_SkeletonFrameReady;
                        replay.ColorImageFrameReady -= replay_ColorImageFrameReady;
                        replay.Stop();
                    }

                    Stream recordStreamReplay = File.OpenRead(sesion_gesto);
                    replay = new KinectReplay(recordStreamReplay);

                    replay.SkeletonFrameReady += replay_SkeletonFrameReady;
                    replay.ColorImageFrameReady += replay_ColorImageFrameReady;
                    gesturesCanvas.Children.Clear();
                    replay.Start();

                    mensajePantalla.Text = "Repetición\n" + articulacion_gesto;

                    cargar_gesto = true;
                }
            }
        }
    }
}