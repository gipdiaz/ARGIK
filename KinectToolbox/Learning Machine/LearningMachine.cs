using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Kinect.Toolbox.Gestures.Learning_Machine;
using Microsoft.Win32;
using System.Windows;
using System;


namespace Kinect.Toolbox
{
    public class LearningMachine
    {
        readonly List<RecordedPath> paths;
        public static int nroGesto = 0;
        public LearningMachine(Stream kbStream)
        {
            if (kbStream == null || kbStream.Length == 0)
            {
                paths = new List<RecordedPath>();
                return;
            }

            BinaryFormatter formatter = new BinaryFormatter { Binder = new CustomBinder() };


            paths = (List<RecordedPath>)formatter.Deserialize(kbStream);
        }

        public List<RecordedPath> Paths
        {
            get { return paths; }
        }
        public string gestoNuevo { get; set; }
        public string repeticion { get; set; }
       

        public string posturaNuevo { get; set; }

        public bool Match(List<Vector2> entries, float threshold, float minimalScore, float minSize)
        {
            return Paths.Any(path => path.Match(entries, threshold, minimalScore, minSize));
        }

        public void Persist(Stream kbStream)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(kbStream, Paths);
        }

        public void AddPath(RecordedPath path)
        {
            path.CloseAndPrepare();
            Paths.Add(path);
            GuardarGesto();
            
        }

        public void GuardarGesto()
        {

            LearningMachine.nroGesto = LearningMachine.nroGesto + 1;
            gestoNuevo = System.IO.Path.Combine(System.Environment.CurrentDirectory, "gesto" + LearningMachine.nroGesto + ".save");
            using (Stream recordStream = File.Create(gestoNuevo))
            {
                Persist(recordStream);
                recordStream.Close();
            }

            repeticion = Microsoft.VisualBasic.Interaction.InputBox("Ingrese cantidad de repeticiones: ");
            
        }
        public void AddPathPosture(RecordedPath path)
        {
            path.CloseAndPrepare();
            Paths.Add(path);
            //GuardarPostura();
        }
        public void GuardarPostura()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog { Title = "Elija nombre de la postura", Filter = "Posturas|*.save" };
            if (saveFileDialog.ShowDialog() == true)
            {
                posturaNuevo = saveFileDialog.FileName;
                using (Stream recordStream = File.Create(posturaNuevo))
                {
                    Persist(recordStream);
                }
            }
        }
    }

}
