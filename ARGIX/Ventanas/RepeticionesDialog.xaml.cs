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
using System.Windows.Shapes;
using System.Windows; // Window, RoutedEventArgs, IInputElement, DependencyObject 
using System.Windows.Controls; // Validation 
using System.Windows.Input; // Keyboard 

namespace ARGIK
{
    /// <summary>
    /// Lógica de interacción para RepeticionesDialog.xaml
    /// </summary>
    public partial class RepeticionesDialog : Window
    {
        public string repeticion { get; set; }
        public RepeticionesDialog()
        {
            InitializeComponent();
            this.boton1.Click += new RoutedEventHandler(boton1_Clicked);
            this.boton2.Click += new RoutedEventHandler(boton2_Clicked);
            this.boton3.Click += new RoutedEventHandler(boton3_Clicked);
        }

        public void boton1_Clicked(object sender, RoutedEventArgs e)
        {
            repeticion = "3";
        }
        public void boton2_Clicked(object sender, RoutedEventArgs e)
        {
            repeticion = "4";
        }
        public void boton3_Clicked(object sender, RoutedEventArgs e)
        {
            repeticion = "5";
        }
    }
}
