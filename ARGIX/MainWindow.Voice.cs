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
            System.Console.WriteLine("Inicio del voice commander");
            voiceCommander.Start(kinectSensor);
        }

        /// <summary>
        /// Verifica si se detecto una orden y realiza la accion correspondiente.
        /// </summary>
        /// <param name="order">La orden.</param>
        public void voiceCommander_OrderDetected(string order)
        {
            System.Console.WriteLine("entro al speech");
            Dispatcher.Invoke(new Action(() =>
            {
                if (audioControl.IsChecked == false)
                {
                    System.Console.WriteLine("roto");
                    return;
                }
                System.Console.WriteLine(order);
                switch (order)
                {
                    case "grabar sesion":
                        DirectRecord(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kinectRecord" + Guid.NewGuid() + ".replay"));
                        break;
                    case "parar sesion":
                        StopRecord();
                        break;
                    case "grabar gesto":
                        grabarListaGestos();
                        break;
                    case "detener gesto":
                        grabarListaGestos();
                        break;

                }
            }));
        }
    }
}
