using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using System.IO;
using Microsoft.Win32;
using Kinect.Toolbox.Voice;
using Coding4Fun.Kinect.Wpf.Controls;
using System.Xml.Serialization;

namespace GesturesViewer
{
    /// <summary>
    /// Pantalla de la aplicacion que permite seleccionar el Joint al cual se le realizara el seguimiento
    /// 
    /// </summary>
    /// 

    public partial class Bienvenida 
    {
        public string jointSeleccionada { get; set; }
        public SerializableDictionary<string, List<string>> b { get; set; } 
        public Bienvenida()
        {
            InitializeComponent();   
            mostrarJoints();
 
        }

        

        /// <summary>
        ///Llena el comboBox con las opciones. Por Default aparecera seleccionada la Mano Derecha. 
        /// </summary>

        public void mostrarJoints()
        {
            jointSeleccion.Items.Add("Cabeza");
            jointSeleccion.Items.Add("Mano Derecha");
            jointSeleccion.Items.Add("Mano Izquierda");
            jointSeleccion.Items.Add("Muñeca Derecha");
            jointSeleccion.Items.Add("Muñeca Izquierda");
            jointSeleccion.Items.Add("Rodilla Derecha");
            jointSeleccion.Items.Add("Rodilla Izquierda");
            jointSeleccion.Items.Add("Pie Derecho");
            jointSeleccion.Items.Add("Pie Izquierdo");
            jointSeleccion.SelectedItem = "Mano Derecha";   
         }
        
        /// <summary>
        /// Metodo que es llamado cuando se realiza el evento de seleccionar una opcion del comboBox. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        public void jointSeleccion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Obtener el  ComboBox.
            var comboBox = sender as ComboBox;

            // obtener el item seleccionado como String.
            jointSeleccionada = comboBox.SelectedItem.ToString();

            }

        /// <summary>
        /// Metodo que es llamado al clickear el boton de iniciar. 
        /// Invoca a la ventana principal pasandole como parametro el nombre del joint que selecciono el usuario
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public void iniciar_Click(object sender, RoutedEventArgs e)
        {
            
            //Serializar el diccionario
            b = new SerializableDictionary<string, List<string>>();
            List <string> medico = new List<string> ();
            medico.Add("German Leschevich");
            List <string> paciente = new List<string> ();
            paciente.Add ("Gaston Diaz");
            List <string> precision = new List<string> ();
            precision.Add ("Media");
            List<string> gestos = new List<string>();
            

            b.Add("Medico", medico);
            b.Add("Paciente", paciente);
            b.Add("Precision", precision);
            b.Add("Gestos", gestos);
           

            

            

            

            
        }
        

         
    }
}
