using System;
using System.IO;

namespace ARGIK
{
    // Esta parte de la clase se encarga de manejar los comandos por voz
    partial class Medico
    {
        /// <summary>
        /// Inicializa el comando por voz
        /// </summary>
        public void StartVoiceCommander()
        {
            voiceCommander.Start(kinectSensor);
        }

        /// <summary>
        /// Verifica si se detecto una orden y realiza la accion correspondiente.
        /// </summary>
        /// <param name="order">La orden.</param>
        public void voiceCommander_OrderDetected(string order)
        {
            System.Console.WriteLine("Orden Detectada");
            Dispatcher.Invoke(new Action(() =>
            {

                System.Console.WriteLine(order);
                switch (order)
                {
                    case "grabar":
                        if (grabando == false && sesionIniciada == true)
                            grabarListaGestos();
                        break;
                    case "detener":
                        if (grabando)
                            grabarListaGestos();
                        break;
                    case "salir":
                        // Sonido
                        mediaPlayer.Open(new Uri(@"../../Media/button-21.mp3", UriKind.Relative));
                        mediaPlayer.Play();
                        this.Clean();
                        MenuPrincipal menuPrincipal = new MenuPrincipal(modoSentado);
                        menuPrincipal.Show();
                        this.Close();
                        break;
                    case "ayuda":
                        // Sonido
                        mediaPlayer.Open(new Uri(@"../../Media/button-30.mp3", UriKind.Relative));
                        mediaPlayer.Play();

                        if (ayudaHabilitada == false)
                            ayudaHabilitada = true;
                        else
                            ayudaHabilitada = false;
                        habilitarAyudas();
                        break;
                }
            }));
        }
    }
}
