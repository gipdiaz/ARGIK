using System;
using System.IO;

namespace GesturesViewer
{
    // Esta parte de la clase se encarga de manejar los comandos por voz
    partial class MainWindow
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
                //audioControl.IsChecked = false
                   // return;

                System.Console.WriteLine(order);
                switch (order)
                {
                    case "grabar":
                        grabarListaGestos();
                        break;
                    case "detener":
                        grabarListaGestos();
                        break;

                }
            }));
        }
    }
}
