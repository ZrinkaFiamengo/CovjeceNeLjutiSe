using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CovjeceNeLjutiSe
{
    /// <summary>
    /// Interaction logic for UnosBox.xaml
    /// </summary>
    public partial class UnosBox : Window
    {
        public UnosBox()
        {
            InitializeComponent();
        }


        public string adresa
        {
            get { return IP_Adresa.Text; }
        }

        private void OK_Click(object sender, RoutedEventArgs e) //kad korisnik pritisne ok zatvori dijalog i vrati kontrolu MainWindowu
        {
            this.Close();
        }

    }
}
