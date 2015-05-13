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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CovjeceNeLjutiSe.Controls
{
    /// <summary>
    /// Interaction logic for field.xaml
    /// </summary>
    public partial class field : UserControl
    {
        public field()
        {
            InitializeComponent();
        }



        public string Boja_pozadine
        {
            get { return (string)GetValue(Boja_pozadineProperty); }
            set { SetValue(Boja_pozadineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Boja_pozadine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Boja_pozadineProperty =
            DependencyProperty.Register("Boja_pozadine", typeof(string), typeof(field), new UIPropertyMetadata("Wheat"));


        public string Slika
        {
            get { return (string)GetValue(SlikaProperty); }
            set { SetValue(SlikaProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Slika.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlikaProperty =
            DependencyProperty.Register("Slika", typeof(string), typeof(field), new UIPropertyMetadata("/Resources/Images/empty.png"));

        
        
    }
}
