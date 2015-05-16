using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CovjeceNeLjutiSe.Controls;
using Microsoft.Win32;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Net;
using System.Net.Sockets;


namespace CovjeceNeLjutiSe
{
    public partial class MainWindow : Window
    {

        //podaci o igračima
        class igraci
        {
            public bool aktivan { get; set; }   //podatak o tome igra li taj igrač ili ne
            public string putanja { get; set; } //putanja do slike igrača
            public int pocetak { get; set; }    //broj polja od kojeg pijun te boje počinje igru
            public string boja { get; set; }    //boja na engleskom
            public string boja_hrv { get; set; }    //boja na hrvatskom
            private int[] prosao = new int[4];  //trenutna udaljenost svakog pijuna od početnog položaja

            //konstruktor novog igrača
            public igraci(string boja, string boja_hrv, bool aktivan, string putanja, int pocetak)
            {
                this.boja = boja;
                this.boja_hrv = boja_hrv;
                this.aktivan = aktivan;
                this.putanja = putanja;
                this.pocetak = pocetak;
                for (int i = 0; i < 4; i++)
                    this.prosao[i] = 0;
            }

            //pomakni pijuna[pozicija] NA udaljenost
            public void setProlazak(int pozicija, int udaljenost)
            {
                prosao[pozicija] = udaljenost;
            }

            //pomakni pijuna[pozicija] ZA udaljenost
            public void addProlazak(int pozicija, int udaljenost)
            {
                prosao[pozicija] += udaljenost;
            }

            //dohvati koliko je prošao pijun[pozicija]
            public int getProlazak(int pozicija)
            {
                return prosao[pozicija];
            }

        };

        string putanja_empty = "/Resources/Images/empty.png";   //putanja do slike na praznom polju
        int player_counter = 0; //informacija o tome koji igrač trenutno igra
        igraci[] igrac = new igraci[4]; // stvori četiri instance klase igrači

        string[] name = new string[4];  //varijabla za pomicanje pijuna, sadrži name trenutne pozicije svakog od pijuna
        string[] new_name = new string[4]; //varijabla za pomicanje pijuna, sadrži name pozicije koju bi svaki od pijuna trebao imati ako ga igrač odabere

        int dice; //vrijednost na kockici
        int broj_na_kockici; //vrijednost na kockici dobivena preko mreže
        int moj_broj; //koji je igrač pokrenut na ovom računalu u slučaju mrežne igre

        Socket sck_s;   //socket na serverskoj strani
        Socket sck_c;   //socket na klijentskoj strani
        Socket[] acc = new Socket[4];   //socketi veze s klijentima na serverskoj strani
        string adresa;  //adresa servera na koju se klijent spaja

        bool Klijent;   //igra je pokrenuta tako da se igra mrežno i igrač je klijent
        bool Server;    //igra je pokrenuta tako da se igra mrežno i igrač je server
        bool Lokalno;   //igra je pokrenuta tako da se igra lokalno



        //konstruktor prozora
        public MainWindow()
        {
            InitializeComponent();

            //inicijalizira četiri moguća igrača, njihovu boju na engleskom i hrvatskom, to da po defaultu ne igraju, putanju do slike i početno polje
            //0=plavi, 1=zeleni, 2=zuti, 3=crveni
            igrac[0] = new igraci("Blue", "plavi", false, "/Resources/Images/Blue/blue_smile.png", 0);
            igrac[1] = new igraci("Green", "zeleni", false, "/Resources/Images/Green/green_smile.png", 12);
            igrac[2] = new igraci("Yellow", "žuti", false, "/Resources/Images/Yellow/yellow_smile.png", 24);
            igrac[3] = new igraci("Red", "crveni", false, "/Resources/Images/Red/red_smile.png", 36);


            //u izborniku za postavljanje igrača postavlja početnu vrijednost svakog igrača na neaktivno
            for (int i = 0; i < 4; i++)
            {
                string name = "Menu_Boja_" + Convert.ToString(i + 1);
                object obj = FindName(name);
                if (obj != null)
                {
                    var menu_item = obj as MenuItem;
                    menu_item.IsChecked = false;
                }
            }

            //pokreni sockete
            sck_s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck_c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            for (int i = 0; i < 4; i++)
                acc[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        //postavlja igrača kojeg korisnik odabere u izborniku na aktivan
        private void Postavi_Igraca(object sender, RoutedEventArgs e)
        {
            var menu_item = sender as MenuItem;
            for (int i = 0; i < 4; i++)
            {
                string broj = Convert.ToString(i + 1);
                if (menu_item.Name.Contains(broj))  //menu_item.Name="Menu_Boja_broj"
                    igrac[i].aktivan = true;    //postavi tog igrača kao aktivnog
            }
        }

        //postavlja igrača kojeg korisnik odabere u izborniku na neaktivan
        private void Ukloni_Igraca(object sender, RoutedEventArgs e)
        {
            var menu_item = sender as MenuItem;
            for (int i = 0; i < 4; i++)
            {
                string broj = Convert.ToString(i + 1);
                if (menu_item.Name.Contains(broj))
                    igrac[i].aktivan = false;   //postavi tog igrača kao neaktivnog
            }
        }

        //postavlja korisnički definiranu sliku igrača
        private void DodajSliku_Click(object sender, RoutedEventArgs e)
        {
            //otvori sistemski prozor za oadbir slike
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp;*.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
            if (open.ShowDialog() == true)
            {
                string file_name = open.FileName;
                //otkrij kojem igraču korisnik postavlja sliku
                var menu = sender as MenuItem;
                var nadmenu = menu.Parent as MenuItem;
                for (int i = 0; i < 4; i++)
                {
                    string broj = Convert.ToString(i + 1);
                    if (nadmenu.Name.Contains(broj))    //nadmenu.Name="Menu_Slika_broj"
                        igrac[i].putanja = file_name;   //postavi sliku tog igrača na onu koju je korisnik odabrao
                }
            }

        }

        //postavlja neku od sistemskih slika igrača
        private void OdabirSlike_Click(object sender, RoutedEventArgs e)
        {
            //pronđi o kojem igraču je riječ
            var slika = sender as Image;
            var menu = slika.Parent as MenuItem;
            var nadmenu = menu.Parent as MenuItem;
            for (int i = 0; i < 4; i++)
            {
                string broj = Convert.ToString(i + 1);
                if (nadmenu.Name.Contains(broj)) //nadmenu.Name="Menu_Slika_broj"
                    igrac[i].putanja = slika.Source.ToString(); //postavi sliku tog igrača na onu koju je korisnik odabrao
            }
        }

        //kraj igre
        private void Zatvori_Igru(object sender, RoutedEventArgs e)
        {
            Close_Sockets();
            this.Close();
        }

        //zatvori sockete
        void Close_Sockets()
        {
            if (sck_c.Connected)
                sck_c.Disconnect(false);
            for (int i = 0; i < 4; i++)
                if (acc[i].Connected)
                    acc[i].Disconnect(false);
            if (sck_s.Connected)
                sck_s.Disconnect(false);
        }



        /*LOKALNA IGRA*/


        //postavljanje sučelja za novu igru
        private void NovaIgra_Click(object sender, RoutedEventArgs e)
        {
            //postavi zastavicu da se igra lokalno
            Klijent = false;
            Server = false;
            Lokalno = true;

            //ukloni ako je mogućnost bacanja kockice ostala od prethodne igre
            this.Kockica.MouseDown -= new MouseButtonEventHandler(Igraj);

            //zatvori komunikaciju ako je ostala otvorena iz prethodne igre
            Close_Sockets();


            //korisnik može igrati lokalno ako je odabrao bar jednog igrača
            bool flag_active = false;
            for (int i = 0; i < 4; i++)
                if (igrac[i].aktivan)
                    flag_active = true;
            if (!flag_active)
            {
                Opis.Text = "Morate odabrati bar jednog igrača!";
                return;
            }

            //postavljanje izgleda prozora
            foreach (var child in this.PoljeZaIgru.Children)    //za svaku čeliju u gridu
            {
                if (!(child is field)) { continue; }    //pogledaj je li ta čelija polje
                var polje = child as field;

                //pogledaj je li to polje neka od kučica
                bool flag = false;
                for (int i = 0; i < 4; i++)
                {
                    string name_part = igrac[i].boja + "_Home";
                    if (polje.Name.Contains(name_part) && igrac[i].aktivan) //ako je kučica i ako je igrač aktivan
                    {
                        polje.Slika = igrac[i].putanja; //postavi pijuna u kučicu
                        flag = true;
                    }
                }
                if (!flag)
                    polje.Slika = putanja_empty;    //ako nije isprazni polje
            }

            //svakom igraču postavi svakom pijunu broj prođenih polja na 0
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    igrac[i].setProlazak(j, 0);
            player_counter = 0;

            //dok ne dođeš na igrača koji je aktivan vrti po igračima
            while (!igrac[player_counter % 4].aktivan)
                player_counter++;

            //obavijesti korisnika o tome tko igra
            Opis.Text = "Igra " + igrac[player_counter % 4].boja_hrv + "!";
            if (igrac[player_counter % 4].boja.Contains("Yellow"))
                Opis.Foreground = Brushes.DarkGoldenrod;
            else
            {
                SolidColorBrush color = (SolidColorBrush)new BrushConverter().ConvertFromString(igrac[player_counter % 4].boja);
                Opis.Foreground = color;
            }

            //omogući korisniku da klikne na kockicu
            this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
        }


        /*MREŽNA IGRA-SERVER*/


        private void MreznaIgraServer_Click(object sender, RoutedEventArgs e)
        {

            //postavi zastavicu da se igra mrežno kao server
            Klijent = false;
            Server = true;
            Lokalno = false;

            //ukloni ako je mogućnost bacanja kockice ostala od prethodne igre
            this.Kockica.MouseDown -= new MouseButtonEventHandler(Igraj);

            //zatvori komunikaciju ako je ostala otvorena iz prethodne igre
            Close_Sockets();

            //za mrežnu igru potrebna su bar dva odabrana igrača na serverskoj strani
            int flag_active = 0;
            for (int i = 0; i < 4; i++)
                if (igrac[i].aktivan)
                    flag_active++;
            if (flag_active < 2)
            {
                Opis.Text = "Morate odabrati bar dva igrača!";
                return;
            }

            //postavljanje socketa servera
            sck_s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //postavi socket na serveru
            sck_s.Bind(new IPEndPoint(0, 51405)); //spoji socket s IP adresom i portom; 0->server može primiti zahtjev s bilo koje IP adrese
            sck_s.Listen(3);    //postavi server da aktivno sluša hoće li klijenti tražiti spajanje


            bool flag_server_player = true; //prvi od odabranih igrača dodjelit će se serveru
            byte[] buffer;  //varijabla u koju se spremaju promjelji podaci ili podaci koji će se slati
            int rec; //broj znakova u primljenoj poruci;

            for (int j = 0; j < 4; j++)
            {
                if (igrac[j].aktivan)   //ako je igrač odabran
                {
                    if (flag_server_player) //ako je prvi odabrani pridjeli ga serveru
                    {
                        moj_broj = j;
                        Opis.Text = "Ti si " + igrac[j].boja_hrv + "!\r\n";
                        flag_server_player = false;
                    }
                    else //ako je serveru već dodjeljen njegov igrač a ima još aktivnih
                    {
                        acc[j] = sck_s.Accept();    //prihvati spajanje klijenta
                        //pošalji klijentu informaciju o tome koliko je i koji su aktivni igrači
                        for (int i = 0; i < 4; i++) //za svakog igrača
                        {
                            //pošalji podatak o igraču je li aktivan
                            buffer = Encoding.Default.GetBytes(igrac[i].aktivan.ToString());
                            try
                            {
                                acc[j].Send(buffer, 0, buffer.Length, 0);
                            }
                            catch
                            {
                                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                            }
                            //provjeri je li korisnik primio poruku
                            buffer = new byte[255];
                            try
                            {
                                rec = acc[j].Receive(buffer, 0, buffer.Length, 0);
                            }
                            catch
                            {
                                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                            }
                            Array.Resize(ref buffer, rec);
                            if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                            {
                                Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                                return;
                            }
                        }

                        //pošalji korisniku informaciju o tome koji je on igrač
                        buffer = Encoding.Default.GetBytes(j.ToString());
                        try
                        {
                            acc[j].Send(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }
                        //provjeri je li korisnik primio poruku
                        buffer = new byte[255];
                        try
                        {
                            rec = acc[j].Receive(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }
                        Array.Resize(ref buffer, rec);
                        if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                        {
                            Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                            return;
                        }
                    }
                }
            }

            //postavljanje izgleda prozora
            foreach (var child in this.PoljeZaIgru.Children)    //za svaku čeliju u gridu
            {
                if (!(child is field)) { continue; }    //pogledaj je li ta čelija polje
                var polje = child as field;

                //pogledaj je li to polje neka od kučica
                bool flag = false;
                for (int i = 0; i < 4; i++)
                {
                    string name_part = igrac[i].boja + "_Home";
                    if (polje.Name.Contains(name_part) && igrac[i].aktivan) //ako je kučica i ako je igrač aktivan
                    {
                        polje.Slika = igrac[i].putanja; //postavi pijuna u kučicu
                        flag = true;
                    }
                }
                if (!flag)
                    polje.Slika = putanja_empty;    //ako nije isprazni polje
            }

            //svakom igraču postavi svakom pijunu broj prođenih polja na 0
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    igrac[i].setProlazak(j, 0);
            player_counter = 0;

            //dok ne dođeš na igrača koji je aktivan vrti po igračima
            while (!igrac[player_counter % 4].aktivan)
                player_counter++;

            //obavijesti korisnika o tome tko igra
            Opis.Text += "Igra " + igrac[player_counter % 4].boja_hrv + "!";
            if (igrac[player_counter % 4].boja.Contains("Yellow"))
                Opis.Foreground = Brushes.DarkGoldenrod;
            else
            {
                SolidColorBrush color = (SolidColorBrush)new BrushConverter().ConvertFromString(igrac[player_counter % 4].boja);
                Opis.Foreground = color;
            }

            //omogući serveru da klikne na kockicu
            if (moj_broj == player_counter % 4)
                this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
            else
            {
                //ako nije tvoj red moraš preko mreže primiti podatke o tijeku igre
                //prvo primam broj na kockici
                var postavi_broj_na_kockici = new Thread(PostaviBrojNaKockiciServer);
                postavi_broj_na_kockici.Start();
            }
        }

        void PostaviBrojNaKockiciServer()
        {
            byte[] buffer = new byte[255];
            int rec;
            try
            {
                rec = acc[player_counter % 4].Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            string poruka = Encoding.Default.GetString(buffer);
            try
            {
                broj_na_kockici = poruka[0] - '0';
            }
            catch
            {
                return;
            }

            //pošalji klijentu odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                acc[player_counter % 4].Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            this.Dispatcher.Invoke((Action)(() =>
            {
                Opis.Text += "!\r\nDobio je broj " + broj_na_kockici.ToString();
                string putanja_kockice = "/Resources/Images/kockica/Kockica_" + broj_na_kockici.ToString() + ".png";
                Kockica.Source = new BitmapImage(new Uri(putanja_kockice, UriKind.RelativeOrAbsolute));
            }));

            //proslijedi informaciju ostalim klijentima
            for (int i = 0; i < 4; i++)
            {
                if (igrac[i].aktivan && i != (player_counter % 4) && i != moj_broj)
                {
                    buffer = Encoding.Default.GetBytes(broj_na_kockici.ToString());
                    try
                    {
                        acc[i].Send(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }

                    //provjeri je li korisnik primio poruku
                    buffer = new byte[255];
                    try
                    {
                        rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }
                    Array.Resize(ref buffer, rec);
                    if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                    {
                        Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                        return;
                    }
                }
            }
            //Kada klijent pomanke pijuna namjesti svoje sučelje
            var postavi_sucelje = new Thread(PostaviSuceljeServer);
            postavi_sucelje.Start();
        }

        void PostaviSuceljeServer()
        {
            byte[] buffer;
            int rec;
            string poruka;

            //primi podatke o trenutnom polju
            buffer = new byte[255];
            try
            {
                rec = acc[player_counter % 4].Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            string name = Encoding.Default.GetString(buffer);

            //pošalji aktivnom klijentu odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                acc[player_counter % 4].Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }

            //primi podatke o novom polju
            buffer = new byte[255];
            try
            {
                rec = acc[player_counter % 4].Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            string new_name = Encoding.Default.GetString(buffer);

            //pošalji serveru odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                acc[player_counter % 4].Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }

            //primi podatke o odabranom pijunu
            buffer = new byte[255];
            try
            {
                rec = acc[player_counter % 4].Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            poruka = Encoding.Default.GetString(buffer);
            int odabrani;
            try
            {
                odabrani = poruka[0] - '0';
            }
            catch
            {
                return;
            }

            //pošalji aktivnom klijetnu odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                acc[player_counter % 4].Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }

            this.Dispatcher.Invoke((Action)(() =>
            {
                Opis.Text = "Broj" + odabrani + name + new_name;
            }));

            //proslijedi informaciju ostalim klijentima
            for (int i = 0; i < 4; i++)
            {
                if (igrac[i].aktivan && i != (player_counter % 4) && i != moj_broj)
                {
                    //pošalji podatak o trenutnom polju
                    buffer = Encoding.Default.GetBytes(name);
                    try
                    {
                        acc[i].Send(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }

                    //provjeri je li korisnik primio poruku
                    buffer = new byte[255];
                    try
                    {
                        rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }
                    Array.Resize(ref buffer, rec);
                    if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                    {
                        Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                        return;
                    }

                    //pošalji podatak o novom polju
                    buffer = Encoding.Default.GetBytes(new_name);
                    try
                    {
                        acc[i].Send(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }

                    //provjeri je li korisnik primio poruku
                    buffer = new byte[255];
                    try
                    {
                        rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }
                    Array.Resize(ref buffer, rec);
                    if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                    {
                        Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                        return;
                    }

                    //pošalji podatak o trenutnom pijunu
                    buffer = Encoding.Default.GetBytes(odabrani.ToString());
                    try
                    {
                        acc[i].Send(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }

                    //provjeri je li korisnik primio poruku
                    buffer = new byte[255];
                    try
                    {
                        rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                    }
                    catch
                    {
                        MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                    }
                    Array.Resize(ref buffer, rec);
                    if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                    {
                        Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                        return;
                    }
                }
            }
            this.Dispatcher.Invoke((Action)(() =>
            {

                bool sestica = (broj_na_kockici == 6); //flag koji provjerava je li broj na kockici jednak 6

                object obj = FindName(name);  //pronađi trenutno polje
                var polje = obj as field;

                object obj2 = FindName(new_name);  //pronađi iduće polje
                if (obj2 != null)
                {
                    var novo_polje = obj2 as field;
                    if (novo_polje.Slika.Contains(igrac[player_counter % 4].putanja) == false)
                    {

                        //pogledaj nalazi se na odabranom polju pijun nekog drugog igrača
                        for (int i = 0; i < 4; i++) //za svakog igrača
                            //ako to nije trenutni igrač i ako na novom polju postoji pijun tog igrača
                            if (i != (player_counter % 4) && novo_polje.Slika.Contains(igrac[i].putanja))
                            {
                                //za svaki od pijuna tog igrača
                                for (int j = 0; j < 4; j++)
                                {
                                    string name_pijun = "Polje_" + Convert.ToString((igrac[i].pocetak + igrac[i].getProlazak(j)) % 49);
                                    //provjeri nalazi li se taj pijun na novom polju trenutnog igrača
                                    if (novo_polje.Name.Contains(name_pijun))
                                    {
                                        //ako je prebaci tog igrača u kučicu
                                        object objec = FindName(igrac[i].boja + "_Home_" + Convert.ToString(j + 1));
                                        var polje_home = objec as field;
                                        polje_home.Slika = igrac[i].putanja;
                                        novo_polje.Slika = putanja_empty; // isprazni polje
                                        igrac[i].setProlazak(j, 0); //postavi tom pijunu da nije prošao ništa puta
                                        i = j = 4;  //i zatvori petlju
                                    }
                                }
                            }

                        polje.Slika = putanja_empty; //isprazni polje na kojem se trenutno nalazi pijun
                        novo_polje.Slika = igrac[player_counter % 4].putanja;   //postavi pijuna na iduće polje
                        //ako je ovaj potez bio izlazak iz kučice, onda se pijun postavlja na prvo mjesto, bez obzira na to koji je broj bio na kockici
                        if (igrac[player_counter % 4].getProlazak(odabrani) == 0)
                            broj_na_kockici = 1;
                        //ako je broj polja prelazio početno polje, povećaj broj koraka koliko se pijun pomaknuo
                        if (igrac[player_counter % 4].pocetak + igrac[player_counter % 4].getProlazak(odabrani) <= 48 && igrac[player_counter % 4].pocetak + igrac[player_counter % 4].getProlazak(odabrani) + dice > 48)
                            broj_na_kockici++;
                        //dodaj pomak broju prijeđenih polja
                        igrac[player_counter % 4].addProlazak(odabrani, broj_na_kockici);

                        //Provjeri je li igrač pobjedio (jesu li svi pijuni u kučici)
                        bool pobjedio = true;
                        for (int i = 0; i < 4; i++)
                            if (igrac[player_counter % 4].getProlazak(i) <= 49)
                                pobjedio = false;
                        //ako je pobjedio obavijesti o pobjedi
                        if (pobjedio)
                        {
                            MessageBoxResult result = MessageBox.Show(igrac[player_counter % 4].boja_hrv.ToUpper() + " je pobjedio!", "Pobjeda!");
                            //zatvori komunikaciju
                            Close_Sockets();
                            Opis.Text = "";
                        }
                    }
                }
                //ako igrač nije dobio šesticu prijeđi na sljedećeg aktivnog igrača
                //(igrač koji je dobio šesticu ima pravo ponovo igrati)
                if (!sestica)
                {
                    player_counter++;
                    while (!igrac[player_counter % 4].aktivan)
                        player_counter++;
                }
                Opis.Text = "Ti si " + igrac[moj_broj].boja_hrv + "!\r\n" + " Igra " + igrac[player_counter % 4].boja_hrv + "!";
                if (igrac[player_counter % 4].boja.Contains("Yellow"))
                    Opis.Foreground = Brushes.DarkGoldenrod;
                else
                {
                    SolidColorBrush color = (SolidColorBrush)new BrushConverter().ConvertFromString(igrac[player_counter % 4].boja);
                    Opis.Foreground = color;
                }
                string putanja_kockice = "/Resources/Images/kockica/Kockica.png";
                Kockica.Source = new BitmapImage(new Uri(putanja_kockice, UriKind.RelativeOrAbsolute));
            }));

            //ako je njegov red omogući korisniku da klikne na kockicu
            if (moj_broj == player_counter % 4)
                this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
            else
            {
                //ako nije tvoj red moraš preko mreže primiti podatke o tijeku igre
                //prvo primam broj na kockici
                var postavi_broj_na_kockici = new Thread(PostaviBrojNaKockiciServer);
                postavi_broj_na_kockici.Start();
            }
        }

        /*MREŽNA IGRA-KLIJENT*/


        private void MreznaIgraKlijent_Click(object sender, RoutedEventArgs e)
        {

            //postavi zastavicu da se igra mrežno kao klijent
            Klijent = true;
            Server = false;
            Lokalno = false;

            //ukloni ako je mogućnost bacanja kockice ostala od prethodne igre
            this.Kockica.MouseDown -= new MouseButtonEventHandler(Igraj);

            //zatvori komunikaciju ako je ostala otvorena iz prethodne igre
            Close_Sockets();

            //otvori korisniku dijalog koji traži od njega da unese IP adresu servera
            var input_box = new UnosBox();
            if (input_box.ShowDialog() == false)    //kad korisnik zatvor dijalog
                adresa = input_box.adresa;  //postavi unesenu vrijednost kao adresu servera

            try //pokušaj se spojiti na odabrani socket
            {
                sck_c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //postavi socket na klijentu
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(adresa), 51405);   //stvori kontekst socketa servera
                sck_c.Connect(endPoint);    //poveži se na server
            }
            catch //obavijesti korisnika o grešci 
            {
                Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                return;
            }

            byte[] buffer; //varijabla u koju se spremaju promjelji podaci ili podaci koji će se slati
            int rec;    //duljina primljenih podataka
            string poruka;  //string vrijednost primljene poruke



            //primanje podataka o tome koji je igrač aktivan a koji ne
            for (int i = 0; i < 4; i++)
            {
                buffer = new byte[255];

                try
                {
                    sck_c.ReceiveTimeout = 5000;    //čekaj max 5sec na odgovor servera
                    rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
                }
                catch //primjeti grešku, dogodit će se kad npr nema više slobodnih igrača (server neće poslati informacije prekobrojnom klijentu i izbacit će ga timeout
                {
                    Opis.Text = "Ne mogu dobiti odgovor od servera!\r\nProvjeri ima li slobodnih igrača!";
                    return;
                }
                try
                {
                    sck_c.ReceiveTimeout = 10 * 60 * 1000;  //ako si se prvi put uspješno povezao, sad podigni timeout na 10 min
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
                Array.Resize(ref buffer, rec);
                poruka = Encoding.Default.GetString(buffer);

                //postavi vrijednost checkboxa u menuu->postavljanje vrijednosti automatski pokreće funkciju koja postavlja i atribut aktivan tog igrača
                object objec = FindName("Menu_Boja_" + Convert.ToString(i + 1));
                var menu = objec as MenuItem;
                menu.IsChecked = poruka.Contains("True");

                //pošalji serveru odgovor da si primio poruku
                buffer = Encoding.Default.GetBytes("MessageAccepted");
                try
                {
                    sck_c.Send(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
            }

            //primi od servera informaciju o tome koji si igač
            buffer = new byte[255];
            try
            {
                rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            poruka = Encoding.Default.GetString(buffer);
            moj_broj = poruka[0] - '0';

            //pošalji serveru odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                sck_c.Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }


            //postavljanje izgleda prozora
            foreach (var child in this.PoljeZaIgru.Children)    //za svaku čeliju u gridu
            {
                if (!(child is field)) { continue; }    //pogledaj je li ta čelija polje
                var polje = child as field;

                //pogledaj je li to polje neka od kućica
                bool flag = false;
                for (int i = 0; i < 4; i++)
                {
                    string name_part = igrac[i].boja + "_Home";
                    if (polje.Name.Contains(name_part) && igrac[i].aktivan) //ako je kučica i ako je igrač aktivan
                    {
                        polje.Slika = igrac[i].putanja; //postavi pijuna u kučicu
                        flag = true;
                    }
                }
                if (!flag)
                    polje.Slika = putanja_empty;    //ako nije isprazni polje
            }

            //svakom igraču postavi svakom pijunu broj prođenih polja na 0
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    igrac[i].setProlazak(j, 0);
            player_counter = 0;

            //dok ne dođeš na igrača koji je aktivan vrti po igračima
            while (!igrac[player_counter % 4].aktivan)
                player_counter++;

            //obavijesti korisnika o tome tko igra
            Opis.Text = "Ti si " + igrac[moj_broj].boja_hrv + "!\r\n" + "Igra " + igrac[player_counter % 4].boja_hrv + "!";
            if (igrac[player_counter % 4].boja.Contains("Yellow"))
                Opis.Foreground = Brushes.DarkGoldenrod;
            else
            {
                SolidColorBrush color = (SolidColorBrush)new BrushConverter().ConvertFromString(igrac[player_counter % 4].boja);
                Opis.Foreground = color;
            }
            //ako je njegov red omogući korisniku da klikne na kockicu
            if (moj_broj == player_counter % 4)
                this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
            else
            {
                //ako nije tvoj red moraš preko mreže primiti podatke o tijeku igre
                //prvo primam broj na kockici
                var postavi_broj_na_kockici = new Thread(PostaviBrojNaKockiciKlijent);
                postavi_broj_na_kockici.Start();
            }
        }


        void PostaviBrojNaKockiciKlijent()
        {
            byte[] buffer = new byte[255];
            int rec;
            try
            {
                rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            string poruka = Encoding.Default.GetString(buffer);
            try
            {
                broj_na_kockici = poruka[0] - '0';
            }
            catch
            {
                return;
            }

            //pošalji serveru odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                sck_c.Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            this.Dispatcher.Invoke((Action)(() =>
            {
                Opis.Text += "!\r\nDobio je broj " + broj_na_kockici.ToString();
                string putanja_kockice = "/Resources/Images/kockica/Kockica_" + broj_na_kockici.ToString() + ".png";
                Kockica.Source = new BitmapImage(new Uri(putanja_kockice, UriKind.RelativeOrAbsolute));
            }));

            //Kada server pomanke pijuna namjesti svoje sučelje
            var postavi_sucelje = new Thread(PostaviSuceljeKlijent);
            postavi_sucelje.Start();
        }

        void PostaviSuceljeKlijent()
        {
            //primi podatke o odabranom pijunu
            byte[] buffer;
            int rec;
            string poruka;

            //primi podatke o trenutnom polju
            buffer = new byte[255];
            try
            {
                rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            string name = Encoding.Default.GetString(buffer);

            //pošalji serveru odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                sck_c.Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }

            //primi podatke o novom polju
            buffer = new byte[255];
            try
            {
                rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            string new_name = Encoding.Default.GetString(buffer);

            //pošalji serveru odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                sck_c.Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }


            //primi podatke o odabranom pijunu
            buffer = new byte[255];
            try
            {
                rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }
            Array.Resize(ref buffer, rec);
            poruka = Encoding.Default.GetString(buffer);
            int odabrani;
            try
            {
                odabrani = poruka[0] - '0';
            }
            catch
            {
                return;
            }

            //pošalji serveru odgovor da si primio poruku
            buffer = Encoding.Default.GetBytes("MessageAccepted");
            try
            {
                sck_c.Send(buffer, 0, buffer.Length, 0);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
            }


            this.Dispatcher.Invoke((Action)(() =>
            {

                bool sestica = (broj_na_kockici == 6); //flag koji provjerava je li broj na kockici jednak 6

                object obj = FindName(name);  //pronađi trenutno polje
                var polje = obj as field;

                object obj2 = FindName(new_name);  //pronađi iduće polje
                if (obj2 != null)
                {
                    var novo_polje = obj2 as field;
                    if (novo_polje.Slika.Contains(igrac[player_counter % 4].putanja) == false)
                    {
                        //pogledaj nalazi se na odabranom polju pijun nekog drugog igrača
                        for (int i = 0; i < 4; i++) //za svakog igrača
                            //ako to nije trenutni igrač i ako na novom polju postoji pijun tog igrača
                            if (i != (player_counter % 4) && novo_polje.Slika.Contains(igrac[i].putanja))
                            {
                                //za svaki od pijuna tog igrača
                                for (int j = 0; j < 4; j++)
                                {
                                    string name_pijun = "Polje_" + Convert.ToString((igrac[i].pocetak + igrac[i].getProlazak(j)) % 49);
                                    //provjeri nalazi li se taj pijun na novom polju trenutnog igrača
                                    if (novo_polje.Name.Contains(name_pijun))
                                    {
                                        //ako je prebaci tog igrača u kučicu
                                        object objec = FindName(igrac[i].boja + "_Home_" + Convert.ToString(j + 1));
                                        var polje_home = objec as field;
                                        polje_home.Slika = igrac[i].putanja;
                                        igrac[i].setProlazak(j, 0); //postavi tom pijunu da nije prošao ništa puta
                                        i = j = 4;  //i zatvori petlju
                                    }
                                }
                            }

                        polje.Slika = putanja_empty; //isprazni polje na kojem se trenutno nalazi pijun
                        novo_polje.Slika = igrac[player_counter % 4].putanja;   //postavi pijuna na iduće polje
                        //ako je ovaj potez bio izlazak iz kučice, onda se pijun postavlja na prvo mjesto, bez obzira na to koji je broj bio na kockici
                        if (igrac[player_counter % 4].getProlazak(odabrani) == 0)
                            broj_na_kockici = 1;
                        //ako je broj polja prelazio početno polje, povećaj broj koraka koliko se pijun pomaknuo
                        if (igrac[player_counter % 4].pocetak + igrac[player_counter % 4].getProlazak(odabrani) <= 48 && igrac[player_counter % 4].pocetak + igrac[player_counter % 4].getProlazak(odabrani) + dice > 48)
                            broj_na_kockici++;
                        //dodaj pomak broju prijeđenih polja
                        igrac[player_counter % 4].addProlazak(odabrani, broj_na_kockici);

                        //Provjeri je li igrač pobjedio (jesu li svi pijuni u kučici)
                        bool pobjedio = true;
                        for (int i = 0; i < 4; i++)
                            if (igrac[player_counter % 4].getProlazak(i) <= 49)
                                pobjedio = false;
                        //ako je pobjedio obavijesti o pobjedi
                        if (pobjedio)
                        {
                            MessageBoxResult result = MessageBox.Show(igrac[player_counter % 4].boja_hrv.ToUpper() + " je pobjedio!", "Pobjeda!");
                            //zatvori komunikaciju
                            Close_Sockets();
                            Opis.Text = "";
                        }
                    }
                }

                //ako igrač nije dobio šesticu prijeđi na sljedećeg aktivnog igrača
                //(igrač koji je dobio šesticu ima pravo ponovo igrati)
                if (!sestica)
                {
                    player_counter++;
                    while (!igrac[player_counter % 4].aktivan)
                        player_counter++;
                }
                Opis.Text = "Ti si " + igrac[moj_broj].boja_hrv + "!\r\n" + " Igra " + igrac[player_counter % 4].boja_hrv + "!";
                if (igrac[player_counter % 4].boja.Contains("Yellow"))
                    Opis.Foreground = Brushes.DarkGoldenrod;
                else
                {
                    SolidColorBrush color = (SolidColorBrush)new BrushConverter().ConvertFromString(igrac[player_counter % 4].boja);
                    Opis.Foreground = color;
                }
                string putanja_kockice = "/Resources/Images/kockica/Kockica.png";
                Kockica.Source = new BitmapImage(new Uri(putanja_kockice, UriKind.RelativeOrAbsolute));
            }));

            //ako je njegov red omogući korisniku da klikne na kockicu
            if (moj_broj == player_counter % 4)
                this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
            else
            {
                //ako nije tvoj red moraš preko mreže primiti podatke o tijeku igre
                //prvo primam broj na kockici
                var postavi_broj_na_kockici = new Thread(PostaviBrojNaKockiciKlijent);
                postavi_broj_na_kockici.Start();
            }
        }



        //***FUNKCIONALNOST IGRE***

        //korisnik je kliknuo na kockicu
        private void Igraj(object sender, MouseButtonEventArgs e)
        {
            //onemogući klikanje na kockicu
            this.Kockica.MouseDown -= new MouseButtonEventHandler(Igraj);
            //odaberi nasumični broj 1-6
            Random rnd = new Random();
            dice = rnd.Next(1, 7);
            string putanja_kockice = "/Resources/Images/kockica/Kockica_" + dice.ToString() + ".png";
            Kockica.Source = new BitmapImage(new Uri(putanja_kockice, UriKind.RelativeOrAbsolute));
            //ispiši korisniku poruku o tome koji je broj dobio i omogući mu klik na svakog od pijuna
            Opis.Text = "Dobili ste broj: " + dice.ToString() + "\r\n" + "Odaberite pijuna";
            //ako si klijent pošalji serveru broj koji si dobio
            if (Klijent)
            {
                byte[] buffer;
                int rec;
                buffer = Encoding.Default.GetBytes(dice.ToString());
                try
                {
                    sck_c.Send(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
                //provjeri je li server primio poruku
                buffer = new byte[255];
                try
                {
                    rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
                Array.Resize(ref buffer, rec);
                if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                {
                    Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                    return;
                }
            }

            //ako si server pošalji svim klijentima broj koji si dobio
            if (Server)
            {

                byte[] buffer;
                int rec;
                for (int i = 0; i < 4; i++)
                {
                    if (igrac[i].aktivan && i != (player_counter % 4) && i != moj_broj)
                    {
                        buffer = Encoding.Default.GetBytes(dice.ToString());
                        try
                        {
                            acc[i].Send(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }

                        //provjeri je li korisnik primio poruku
                        buffer = new byte[255];
                        try
                        {
                            rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }
                        Array.Resize(ref buffer, rec);
                        if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                        {
                            Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                            return;
                        }
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                int igram = player_counter % 4; //trenutni igrač
                int prosao = igrac[igram].getProlazak(i);   //broj polja koliko je do sad igrač prošao
                int fix = (igrac[igram].pocetak + prosao) / 49;   //varijabla koja određuje je li igrač prešao početno polje (0 ako nije, broj polja koliko je prešao ako je)
                if (prosao == 0)
                {
                    //ako je pijun još u kučici
                    name[i] = igrac[igram].boja + "_Home_" + Convert.ToString(i + 1); //polje na kojem se trenutno nalazi je "Boja_Home_Broj"
                    //ako je igrač dobio broj 6 izlazi iz kučice
                    if (dice == 6)
                        new_name[i] = "Polje_" + Convert.ToString(igrac[igram].pocetak + 1); //iduće polje je polje njegovog početka
                    else
                        new_name[i] = "Skip_play"; //inače igrač preskače red
                }
                else if (prosao - fix > 48)
                {
                    //ako je pijun u cilju
                    name[i] = igrac[igram].boja + "_Finish_" + Convert.ToString(prosao - fix - 48); //polje na kojem se trenutno nalazi je "Boja_Finish_Broj"
                    //ako je broj na kockici manji ili jednak broju polja u cilju koliko još ima ispred njega
                    if (prosao - fix - 48 + dice <= 4)
                        new_name[i] = igrac[igram].boja + "_Finish_" + Convert.ToString(prosao - fix - 48 + dice); //iduće polje je jedno od polja cilja
                    else
                        new_name[i] = "OutOfRange"; //inače nema polja na koje možemo pomaknuti tog pijuna
                }
                else
                {
                    //ako je pijun unutar polja za igru
                    name[i] = "Polje_" + Convert.ToString((igrac[igram].pocetak + prosao) % 49);//polje na kojem se trenutno nalazi je "Polje_Broj"
                    //ako pijun nakon dodavanja broja na kockici ostaje u polju za igru
                    if (prosao - fix + dice <= 48)
                    {
                        int smjesten;
                        smjesten = (igrac[igram].pocetak + prosao + dice) % 49; //polje na koje će se igrač postaviti
                        //provjera prelazi li pijun u trenutnom potezu kroz početno polje
                        //razlog provjere je to što u igri postoje polja 1-48, a kao ostatak djeljenja nekog broja s 49 moguće je dobiti ostatak nula
                        //npr. ako se pijun nalazi na polju_48 i dobije 1, on bi trebao ići na polje_0 koje ne postoji nego umjesto toga ide na
                        //jedno polje više, tj polje_1 (isto vrijedi za svaki prolazak kroz početno polje)
                        if (igrac[igram].pocetak + prosao <= 48 && igrac[igram].pocetak + prosao + dice > 48)
                            smjesten++;
                        //Iduće polje je jedno od polja za igru
                        new_name[i] = "Polje_" + Convert.ToString(smjesten);
                    }
                    //ako pijun nakon dodavanja broja na kockici ide u cilj
                    else if (prosao - fix + dice <= 48 + 4)
                    {
                        //sljedeće polje je jedno od polja u cilju
                        new_name[i] = igrac[igram].boja + "_Finish_" + Convert.ToString(prosao - fix - 48 + dice);
                    }
                    //inače pijun nakon dodavanja broja na kockici izlazi iz granica igre i ne može se pomaknuti naprijed
                    else
                        new_name[i] = "OutOfRange";
                }
                //pronađi polje na kojem se pijun trenutno nalazi i dopusti korisniku da klikne na to polje
                object obj = FindName(name[i]);
                if (obj != null)
                {
                    var polje = obj as field;
                    polje.MouseDown += new MouseButtonEventHandler(polje_MouseDown);
                }
            }
        }

        private void polje_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //korisnik je odabrao pomicanje jednog od pijuna
            bool sestica = (dice == 6); //flag koji provjerava je li broj na kockici jednak 6
            var polje = sender as field;
            //provjeri je li polje na koje je korisnik kliknuo jedno od mogućih polja na koja je korisniku dopušteno kliknuti
            int odabrani = -1;
            for (int i = 0; i < 4; i++)
                if (polje.Name.Equals(name[i]))
                    odabrani = i;
            if (odabrani == -1)
                return;
            //provjeri postoji li pijun na kojeg korisnik može kliknuti i pomaknuti ga
            bool postoje_potezi = false;
            for (int i = 0; i < 4; i++)
            {
                object obj = FindName(new_name[i]);
                if (obj != null)   //rezultat je null ako je new_name "Skip_play" ili "OutOfRange" jer polja s tim imenom ne postoje
                {
                    var polje2 = obj as field;
                    if (polje2.Slika.Contains(igrac[player_counter % 4].putanja) == false)   //ako na idućem polju već ne stoji pijun istog igrača
                        postoje_potezi = true;
                }
            }
            if (postoje_potezi)
            {
                //ako postoje potezi koje ovaj igrač može igrati
                //ako trenutno odabrani potez nije jedan od njih, završi funkciju i dopusti igraču da ponovo bira
                if (new_name[odabrani].Contains("Skip_play") || new_name[odabrani].Contains("OutOfRange"))
                    return;
                object obj = FindName(new_name[odabrani]);  //pronađi iduće polje
                if (obj != null)
                {
                    var novo_polje = obj as field;
                    //ako se na idućem polju nalazi pijun istog igrača izađi iz funkcije i pusti igrača da odabere drugi pijun
                    if (novo_polje.Slika.Contains(igrac[player_counter % 4].putanja))
                    {
                        return;
                    }
                    //inače potez kojeg je igrač upravo napravio odgovara uvjetima i može se izvršiti
                    Opis.Text = "";
                    //pogledaj nalazi se na odabranom polju pijun nekog drugog igrača
                    for (int i = 0; i < 4; i++) //za svakog igrača
                        //ako to nije trenutni igrač i ako na novom polju postoji pijun tog igrača
                        if (i != (player_counter % 4) && novo_polje.Slika.Contains(igrac[i].putanja))
                        {
                            //za svaki od pijuna tog igrača
                            for (int j = 0; j < 4; j++)
                            {
                                string name = "Polje_" + Convert.ToString((igrac[i].pocetak + igrac[i].getProlazak(j)) % 49);
                                //provjeri nalazi li se taj pijun na novom polju trenutnog igrača
                                if (novo_polje.Name.Contains(name))
                                {
                                    //ako je prebaci tog igrača u kučicu
                                    object objec = FindName(igrac[i].boja + "_Home_" + Convert.ToString(j + 1));
                                    var polje_home = objec as field;
                                    polje_home.Slika = igrac[i].putanja;
                                    novo_polje.Slika = putanja_empty; // isprazni polje
                                    igrac[i].setProlazak(j, 0); //postavi tom pijunu da nije prošao ništa puta
                                    i = j = 4;  //i zatvori petlju
                                }
                            }
                        }
                    polje.Slika = putanja_empty; //isprazni polje na kojem se trenutno nalazi pijun
                    novo_polje.Slika = igrac[player_counter % 4].putanja;   //postavi pijuna na iduće polje
                    //ako je ovaj potez bio izlazak iz kučice, onda se pijun postavlja na prvo mjesto, bez obzira na to koji je broj bio na kockici
                    if (igrac[player_counter % 4].getProlazak(odabrani) == 0)
                        dice = 1;
                    //ako je broj polja prelazio početno polje, povećaj broj koraka koliko se pijun pomaknuo
                    if (igrac[player_counter % 4].pocetak + igrac[player_counter % 4].getProlazak(odabrani) <= 48 && igrac[player_counter % 4].pocetak + igrac[player_counter % 4].getProlazak(odabrani) + dice > 48)
                        dice++;
                    //dodaj pomak broju prijeđenih polja
                    igrac[player_counter % 4].addProlazak(odabrani, dice);
                }
            }
            else
            {
                //ako ne postoje potezi koje ovaj igrač može odigrati ispiši obavijest
                Opis.Text = "Nemate mogućih poteza!\r\n";
                new_name[odabrani] = "NoMoves!";
            }
            if (Server)
            {
                byte[] buffer;
                int rec;
                for (int i = 0; i < 4; i++)
                {
                    if (igrac[i].aktivan && i != (player_counter % 4) && i != moj_broj)
                    {
                        //pošalji korisnicima koje je trenutno polje
                        buffer = Encoding.Default.GetBytes(name[odabrani]);
                        try
                        {
                            acc[i].Send(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }

                        //provjeri je li korisnik primio poruku
                        buffer = new byte[255];
                        try
                        {
                            rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }
                        Array.Resize(ref buffer, rec);
                        if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                        {
                            Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                            return;
                        }

                        //pošalji korisnicima koje je iduće polje
                        if (igrac[i].aktivan && i != (player_counter % 4) && i != moj_broj)
                        {
                            buffer = Encoding.Default.GetBytes(new_name[odabrani]);
                            try
                            {
                                acc[i].Send(buffer, 0, buffer.Length, 0);
                            }
                            catch
                            {
                                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                            }

                            //provjeri je li korisnik primio poruku
                            buffer = new byte[255];
                            try
                            {
                                rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                            }
                            catch
                            {
                                MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                            }
                            Array.Resize(ref buffer, rec);
                            if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                            {
                                Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                                return;
                            }
                        }

                        //posalji korisnicima koji je pijun odabran
                        buffer = Encoding.Default.GetBytes(odabrani.ToString());
                        try
                        {
                            acc[i].Send(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }

                        //provjeri je li korisnik primio poruku
                        buffer = new byte[255];
                        try
                        {
                            rec = acc[i].Receive(buffer, 0, buffer.Length, 0);
                        }
                        catch
                        {
                            MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                        }
                        Array.Resize(ref buffer, rec);
                        if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                        {
                            Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                            return;
                        }
                    }
                }
            }
            if (Klijent)
            {
                byte[] buffer;
                int rec;

                //posalji serveru trenutno polje
                buffer = Encoding.Default.GetBytes(name[odabrani]);
                try
                {
                    sck_c.Send(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }

                //provjeri je li server primio poruku
                buffer = new byte[255];
                try
                {
                    rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
                Array.Resize(ref buffer, rec);
                if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                {
                    Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                    return;
                }

                //posalji serveru iduće polje
                buffer = Encoding.Default.GetBytes(new_name[odabrani]);
                try
                {
                    sck_c.Send(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }

                //provjeri je li server primio poruku
                buffer = new byte[255];
                try
                {
                    rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
                Array.Resize(ref buffer, rec);
                if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                {
                    Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                    return;
                }

                //posalji serveru koji je pijun odabran
                buffer = Encoding.Default.GetBytes(odabrani.ToString());
                try
                {
                    sck_c.Send(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }

                //provjeri je li server primio poruku
                buffer = new byte[255];
                try
                {
                    rec = sck_c.Receive(buffer, 0, buffer.Length, 0);
                }
                catch
                {
                    MessageBoxResult result = MessageBox.Show("Ups! Došlo je do greške!", "Greska pri povezivanju!"); return;
                }
                Array.Resize(ref buffer, rec);
                if (Encoding.Default.GetString(buffer).Contains("MessageAccepted") == false)
                {
                    Opis.Text = "Greska pri povezivanju!\r\nPokušaj ponovo!";
                    return;
                }
            }
            //skini mogučnost reakcije na klik sa svih pijuna ovog igrača
            for (int i = 0; i < 4; i++)
            {
                object obj1 = FindName(name[i]);
                if (obj1 != null)
                {
                    var polja = obj1 as field;
                    polja.MouseDown -= new MouseButtonEventHandler(polje_MouseDown);
                }
            }
            //Provjeri je li igrač pobjedio (jesu li svi pijuni u kučici)
            bool pobjedio = true;
            for (int i = 0; i < 4; i++)
                if (igrac[player_counter % 4].getProlazak(i) <= 49)
                    pobjedio = false;
            //ako je pobjedio obavijesti o pobjedi
            if (pobjedio)
            {
                MessageBoxResult result = MessageBox.Show(igrac[player_counter % 4].boja_hrv.ToUpper() + " je pobjedio!", "Pobjeda!");
                //zatvori komunikaciju
                Close_Sockets();
                Opis.Text = "";
            }
            //ako igrač nije dobio šesticu prijeđi na sljedećeg aktivnog igrača
            //(igrač koji je dobio šesticu ima pravo ponovo igrati)
            if (!sestica)
            {
                player_counter++;
                while (!igrac[player_counter % 4].aktivan)
                    player_counter++;
            }

            if (Server || Klijent)
                Opis.Text = "Ti si " + igrac[moj_broj].boja_hrv + "!\r\n" + " Igra " + igrac[player_counter % 4].boja_hrv + "!";
            else
                Opis.Text = " Igra " + igrac[player_counter % 4].boja_hrv + "!";
            if (igrac[player_counter % 4].boja.Contains("Yellow"))
                Opis.Foreground = Brushes.DarkGoldenrod;
            else
            {
                SolidColorBrush color = (SolidColorBrush)new BrushConverter().ConvertFromString(igrac[player_counter % 4].boja);
                Opis.Foreground = color;
            }
            //omogući klik na kockicu
            string putanja_kockice = "/Resources/Images/kockica/Kockica.png";
            Kockica.Source = new BitmapImage(new Uri(putanja_kockice, UriKind.RelativeOrAbsolute));
            if (Lokalno)
            {
                this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
            }
            else
            {
                //omogući klik na kockicu ili čekaj na idućeg igrača
                if (moj_broj == player_counter % 4)
                    this.Kockica.MouseDown += new MouseButtonEventHandler(Igraj);
                else
                {
                    //ako nije tvoj red moraš preko mreže primiti podatke o tijeku igre
                    //prvo primam broj na kockici
                    Thread postavi_broj_na_kockici;
                    if (Server)
                        postavi_broj_na_kockici = new Thread(PostaviBrojNaKockiciServer);
                    else
                        postavi_broj_na_kockici = new Thread(PostaviBrojNaKockiciKlijent);

                    postavi_broj_na_kockici.Start();
                }
            }
        }
    }
}