using System;
using System.IO;

namespace GesturesViewer
{
    partial class MainWindow
    {
        void StartVoiceCommander()
        {
            System.Console.WriteLine("Inicio del voice commander");
            voiceCommander.Start(kinectSensor);
        }

        void voiceCommander_OrderDetected(string order)
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
                    case "grabar":
                        System.Console.WriteLine("entro al grabar");
                        DirectRecord(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kinectRecord" + Guid.NewGuid() + ".replay"));
                        break;
                    case "parar":
                        StopRecord();
                        break;
                }
            }));
        }
    }
}
