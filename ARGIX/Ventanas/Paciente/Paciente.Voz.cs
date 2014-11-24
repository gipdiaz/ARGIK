﻿using System;
using System.IO;

namespace ARGIK
{
    // Esta parte de la clase se encarga de manejar los comandos por voz
    partial class Paciente
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
                    case "reproducir":
                        ReproducirSesion();
                        break;
                    case "detener":
                        
                        detenerSesion();
                        break;
                    case "repetir":
                        if (sesionIniciada)
                        RepetirGesto();
                        break;
                    case "ayuda":
                        if (ayudaHabilitada == false)
                            ayudaHabilitada = true;
                        else
                            ayudaHabilitada = false;
                        habilitarAyudas();
                        break;
                    case "salir":
                        this.Clean();
                        MenuPrincipal menuPrincipal = new MenuPrincipal(modoSentado);
                        menuPrincipal.Show();
                        this.Close();
                        break;
                }
            }));
        }
    }
}
