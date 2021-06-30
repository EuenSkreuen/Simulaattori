using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.IO;

namespace Simulaattori
{
    public partial class Form1 : Form
    {
        public static int resolutionX = 1000;
        public static int resolutionY = 1000;
        public static Bitmap kuva = new Bitmap(resolutionX, resolutionY);        
        Random random = new Random();
        bool visualizeStars = false;
        float globalTime = 0;
        double koko = 50;
        int määrä = 500;
        double epsilon = 0.1;
        bool toistetaan = false;
        double tarkkuusParametri = 0.02; // sivu 8, https://www.researchgate.net/publication/226982628_N-body_simulations_of_gravitational_dynamics
        List<Star> stars;
        float timeStep = 0.1f;
        bool dynamicStepping = false;
        //TODO: muut vakiot, kuten G, pc, jne
        float G = 4.302E-3F;

        public Form1()
        {
            InitializeComponent();
            esikatselu.BackColor = Color.Blue;
            //TODO: Tarkistukset syötteille            
        }

        private void aloitaSimulaatio()
        {
            if (backgroundWorker1.IsBusy)
            {
                button4.Enabled = false;
                backgroundWorker1.CancelAsync();
                button4.Enabled = true;
                button4.Text = "Aloita simulaatio";
                button3.Enabled = true;
                checkBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                checkBox3.Enabled = true;
                textBox1.Enabled = true;
                textBox4.Enabled = true;
                globalTime = 0;
                return;
            }
            try
            {
                koko = Convert.ToDouble(textBox3.Text);
                määrä = Convert.ToInt32(textBox2.Text);
                epsilon = Convert.ToDouble(textBox1.Text);
                tarkkuusParametri = Convert.ToDouble(textBox4.Text);
            }catch
            {
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show("Virheellinen arvo.", "Virhe", buttons);
                return;
            }
            stars = new List<Star>();
            //TODO: Ota syötteistä tiedot asetuksiin ja aloita simulaatio
            //Palauta tästä funktiosta viite laskentaprosessiin  
            //https://math.stackexchange.com/questions/87230/picking-random-points-in-the-volume-of-sphere-with-uniform-probability
            //Tuottaa pallomaisen jakauman
            for (int n=0; n < määrä; n++)
            {
                //Annetaan tähdelle uniikki id jotta se erotetaan muista myöhemmin
                int id = n;
                //Arvotaan tähden sijainti pallon sisällä
                double theta = random.NextDouble() * 2 * Math.PI;
                double v = random.NextDouble();
                double phi = Math.Acos(2 * v - 1);
                double r = koko * Math.Pow(random.NextDouble(), 1 / 3);
                float x = (float)(r * Math.Cos(theta) * Math.Sin(phi));
                float y = (float)(r * Math.Sin(theta) * Math.Sin(phi));
                float z = (float)(r * Math.Cos(phi));
                Vector3 piste = new Vector3(x, y, z);
                //Arvotaan tähden luokkatyyppi 
                //Luokkien suhteelliset määrät laskettu artikkelista http://cdsads.u-strasbg.fr/pdf/2001JRASC..95...32L
                //Laskuissa otettu huomioon vain pääsarjan tähdet
                //O-luokka: jätetty pois harvinaisuuden takia
                //B-luokka : 0.12%
                //A-luokka : 0.61%
                //F-luokka : 3.03%
                //G-luokka : 7.65%
                //K-luokka : 12.14%
                //M-luokka : 76.45%
                double luokkaLuku = random.NextDouble() * 100;
                //Tähden luokkatyypistä arvotaan tähdelle massa luokan perusteella.
                //Massat on otettu keskiarvoina kyseisen luokan massoista
                //Massojen pohjalta tähdille on laskettu luminositeetit tähdelle luentomonisteessa mainitun
                //pääsarjaan liittyvän massa-luminositeetti relaation avulla
                float massa = 1f;
                Vector3 c = new Vector3(155, 176, 255);
                if(luokkaLuku < 0.12)
                {
                    //B-luokka
                    //Massat väliltä 2.1-16
                    massa = (float) (random.NextDouble() * (16 - 2.1) + 2.1);
                    c = new Vector3(170, 191, 255);
                }
                if(luokkaLuku >= 0.12 && luokkaLuku < 0.73)
                {
                    //A-luokka
                    //Massat väliltä 1.4-2.1
                    massa = (float)(random.NextDouble() * (2.1 - 1.4) + 1.4);
                    c = new Vector3(202, 215, 255);
                }
                if(luokkaLuku >= 0.73 && luokkaLuku < 3.76)
                {
                    //F-luokka
                    //Massat väliltä 1.04-1.4
                    massa = (float)(random.NextDouble() * (1.4 - 1.04) + 1.04);
                    c = new Vector3(248, 247, 255);
                }
                if(luokkaLuku >= 3.76 && luokkaLuku < 11.41)
                {
                    //G-luokka
                    //Massat väliltä 0.8-1.04
                    massa = (float)(random.NextDouble() * (1.04 - 0.8) + 0.8);
                    c = new Vector3(255, 244, 234);
                }
                if(luokkaLuku >= 11.41 && luokkaLuku < 23.55)
                {
                    //K-luokka
                    //Massat väliltä 0.45-0.8
                    massa = (float)(random.NextDouble() * (0.8 - 0.45) + 0.45);
                    c = new Vector3(255, 210, 161);
                }
                if(luokkaLuku >= 23.55)
                {
                    //M-luokka
                    //Massat väliltä 0.08-0.45
                    massa = (float)(random.NextDouble() * (0.45-0.08) + 0.08);
                    c = new Vector3(255, 204, 111);
                }
                //Luentomateriaalin perusteella massa-luminositeeti relaation kerroin on 3.8
                double luminositeetti = Math.Pow((double)massa, 3.8);
                //Luminositeetin pohjalta voidaan laskea absoluuttinen magnitudi
                //Auringon visuaalinen absoluuttinen magnitudi on 4.82 (luentomateriaali) ja luminositeetti on 1.
                //Tähden absoluuttinen magnitudi on siis
                float absoluuttinenMagnitudi = (float) (-2.5 * Math.Log10(luminositeetti) + 4.82);
                //Tähden näennäinen magnitudi saadaan taas laskettua piirtovaiheessa

                stars.Add(new Star(id, piste, new Vector3(0, 0, 0), new Vector3(0, 0, 0), c, absoluuttinenMagnitudi, massa, 0, 0, 0));
            }            
            if (backgroundWorker1.IsBusy != true)
            {
                button4.Text = "Keskeytä simulaatio";
                button3.Enabled = false;
                checkBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;
                checkBox3.Enabled = false;
                textBox1.Enabled = false;
                textBox4.Enabled = false;
                backgroundWorker1.RunWorkerAsync();
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Tarkistetaan onko tallennettuja simulaatioita, jos on niin toistetaan
            //TODO: tätäkin voisi parannella järkevämmäksi joskus
            //TODO: ohjelma ei salli lopettamista jos käy poistamassa ekan tiedoston ajamisen aikana!!
            if (File.Exists(@"C:\gravitaatiosimulaatiokansio\0.txt"))
            {
                if (backgroundWorker2.IsBusy)
                {                    
                    button3.Enabled = false;
                    backgroundWorker2.CancelAsync();
                    button3.Enabled = true;
                    button3.Text = "Toista simulaatio";
                    button3.Enabled = true;
                    checkBox2.Enabled = true;
                    checkBox1.Enabled = true;
                    textBox2.Enabled = true;
                    textBox3.Enabled = true;
                    checkBox3.Enabled = true;
                    textBox1.Enabled = true;
                    textBox4.Enabled = true;
                    button4.Enabled = true;
                    globalTime = 0;
                    toistetaan = false;
                    return;
                }
                if (backgroundWorker2.IsBusy != true)
                {
                    string[] rivit = File.ReadAllLines(@"C:\gravitaatiosimulaatiokansio\0.txt");
                    string[] metarivi = rivit[0].Split(';');
                    koko = Convert.ToDouble(metarivi[1]);
                    stars = new List<Star>();
                    for (int i = 1; i < rivit.Length; i++)
                    {
                        string[] subs = rivit[i].Split(';');
                        float sx = (float)Convert.ToDouble(subs[0]);
                        float sy = (float)Convert.ToDouble(subs[1]);
                        float sz = (float)Convert.ToDouble(subs[2]);
                        Vector3 sijainti = new Vector3(sx, sy, sz);
                        int cx = (int)Convert.ToInt32(subs[3]);
                        int cy = (int)Convert.ToInt32(subs[4]);
                        int cz = (int)Convert.ToInt32(subs[5]);
                        Vector3 c = new Vector3(cx, cy, cz);
                        float absoluuttinenMagnitudi = (float)Convert.ToDouble(subs[6]);
                        int piirtoX = Convert.ToInt32(subs[7]);
                        int piirtoY = Convert.ToInt32(subs[8]);
                        stars.Add(new Star(0, sijainti, new Vector3(0, 0, 0), new Vector3(0, 0, 0), c, absoluuttinenMagnitudi, 0, 0, piirtoX, piirtoY));
                    }
                    button4.Enabled = false;
                    button3.Text = "Keskeytä toisto";
                    checkBox2.Enabled = false;
                    checkBox1.Enabled = false;
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;
                    checkBox3.Enabled = false;
                    textBox1.Enabled = false;
                    textBox4.Enabled = false;
                    toistetaan = true;
                    backgroundWorker2.RunWorkerAsync();
                }
            }
            else
            {
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show("Virheellinen arvo.", "Virhe", buttons);
                return;
            }                
            
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int iteration = 0;
            while (true)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    if (dynamicStepping)
                    {
                        dynaaminenLaskenta(stars, 0);
                    }
                    else
                    {
                        suoraLaskenta(stars);
                    }
                    if (checkBox1.Checked)
                    {
                        //Tallenetaan simulaation askel
                        tallenna(iteration);
                        iteration++;
                    }
                    worker.ReportProgress(1);                    
                }
            }
        }

        private void tallenna(int frame)
        {
            //TODO: järkevämpi tallennusmekanismi
            //Luodaan tallennuskansio jos sitä ei ole olemassa (joo kiva tarkistaa joka vitun kerta tää, kyllä tietokone tykkää...)
            if (!Directory.Exists(@"C:\gravitaatiosimulaatiokansio"))
            {
                Directory.CreateDirectory(@"C:\gravitaatiosimulaatiokansio");
            }
            //Tallennetaan tekstitiedosto
            string[] rivit = new string[stars.Count + 1];
            rivit[0] = globalTime + ";" + koko; //Eka rivi on metadataa varten, tässä tapauksessa vain senhetkinen aika
            int i = 1;
            foreach (Star s in stars)
            {
                rivit[i] =
                    //s.id + ";" +
                    s.sijainti.X + ";" + s.sijainti.Y + ";" + s.sijainti.Z + ";" +
                    //s.nopeus.X + ";" + s.nopeus.Y + ";" + s.nopeus.Z + ";" +
                    //s.kiihtyvyys.X + ";" + s.kiihtyvyys.Y + ";" + s.kiihtyvyys.Z + ";" +
                    s.väri.X + ";" + s.väri.Y + ";" + s.väri.Z + ";" +
                    s.absoluuttinenMagnitudi + ";" +
                    //s.massa + ";" +
                    //s.taso + ";" +
                    s.piirtoX + ";" +
                    s.piirtoY;
                i++;
            }
            File.WriteAllLines(@"C:\gravitaatiosimulaatiokansio\" + frame + ".txt", rivit);            
        }

        private void lue(int frame)
        {
            string[] rivit = File.ReadAllLines(@"C:\gravitaatiosimulaatiokansio\" + frame + ".txt");
            string[] metarivi = rivit[0].Split(';');
            globalTime = (float) Convert.ToDouble(metarivi[0]);
            int iterator = 1;
            foreach (Star s in stars)
            {
                string[] subs = rivit[iterator].Split(';');
                float sx = (float) Convert.ToDouble(subs[0]);
                float sy = (float)Convert.ToDouble(subs[1]);
                float sz = (float)Convert.ToDouble(subs[2]);
                s.sijainti = new Vector3(sx, sy, sz);
                int cx = (int)Convert.ToInt32(subs[3]);
                int cy = (int)Convert.ToInt32(subs[4]);
                int cz = (int)Convert.ToInt32(subs[5]);
                s.väri = new Vector3(cx, cy, cz);
                s.absoluuttinenMagnitudi = (float) Convert.ToDouble(subs[6]);
                s.piirtoX = Convert.ToInt32(subs[7]);
                s.piirtoY = Convert.ToInt32(subs[8]);
                iterator++;
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int iteration = 0;
            while (true)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    if(File.Exists(@"C:\gravitaatiosimulaatiokansio\" + iteration + ".txt"))
                    {
                        //Luetaan tiedot taulukkoon, ja päivitetään tähtilistaa
                        lue(iteration);
                    } else
                    {
                        //Mentiin ohi viimeisestä tiedostosta, palataan alkuun
                        iteration = 0;
                        continue;
                    }
                    iteration++;
                    worker.ReportProgress(1);
                    System.Threading.Thread.Sleep(10);
                }
            }
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            piirra(stars);
            esikatselu.Refresh();
            globalTime += timeStep;
            label9.Text = (Math.Floor(globalTime)).ToString() + " vuotta";
            label9.Refresh();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            piirra(stars);
            esikatselu.Refresh();
            globalTime += timeStep;
            label9.Text = (Math.Floor(globalTime)).ToString() + " vuotta";
            label9.Refresh();
        }

        private void piirra(List<Star> stars)
        {
            Graphics grafiikka = Graphics.FromImage(kuva);
            Vector3 cameraPoint = new Vector3(0, 0, -2f * (float)koko);
            if (!toistetaan)
            {                
                Vector3 origo = new Vector3(0, 0, 0);
                float distanceToPlane = 1;
                double maxAngle = Math.Atan(0.5 / 1) * 1.7; //Maksimikulma olettaen että katsoja on 1 päässä tason keskipisteestä joka on 1x1 kokoinen                
                foreach (Star s in stars)
                {
                    double x = s.sijainti.X;
                    double y = s.sijainti.Y;
                    double z = s.sijainti.Z;
                    //Lasketaan x-komponentin kulma kuvatasoon nähden
                    double b = x; //Kolmion vastainen kateetti
                    if (z < cameraPoint.Z)
                    {
                        s.piirtoX = -1;
                        s.piirtoY = -1;
                        continue; //Tähti on kameran takana
                    }
                    double a = Math.Abs(cameraPoint.Z - z); //Kolmion viereinen kateetti              
                    double c = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
                    double angle = Math.Asin(b / c);
                    if (Math.Abs(angle) > maxAngle)
                    {
                        s.piirtoX = -1;
                        s.piirtoY = -1;
                        continue; //Tähti on kuvakulman ulkopuolella x-akselilla
                    }
                    else
                    {
                        double innerDistance = Math.Tan(angle); //Etäisyyskameratason origon ja pikselin välillä
                        s.piirtoX = Convert.ToInt32(Math.Round(0.5 * resolutionX + 0.5 * resolutionX * innerDistance));
                    }
                    //Lasketaan sama y-komponentille
                    b = y; //Kolmion vastainen kateetti                
                    c = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
                    angle = Math.Asin(b / c);
                    if (Math.Abs(angle) > maxAngle)
                    {
                        s.piirtoX = -1;
                        s.piirtoY = -1;
                        continue; //Tähti on kuvakulman ulkopuolella y-akselilla
                    }
                    else
                    {
                        double innerDistance = Math.Tan(angle); //Etäisyys kameratason origon ja pikselin välillä
                        s.piirtoY = Convert.ToInt32(Math.Round(0.5 * resolutionY - 0.5 * resolutionY * innerDistance));
                    }
                    //Tallennetaan tähti annettuun koordinaattiin
                }
            }
            
            grafiikka.FillRectangle(Brushes.Black, 0, 0, resolutionX, resolutionY); //Alustetaan piirros mustaksi
            /*
            grafiikka.FillRectangle(Brushes.Red, 0, 0, 20, 20); //vasen ylänurkka
            grafiikka.FillRectangle(Brushes.Green, resolutionX-10, 0, 20, 20); //oikea ylänurkka
            grafiikka.FillRectangle(Brushes.Blue, resolutionX-10, resolutionY-10, 20, 20); //oikea alanurkka
            grafiikka.FillRectangle(Brushes.White, 0, resolutionY-10, 20, 20); //vasen alanurkka
            */
            foreach (Star s in stars)
            {
                if(s.piirtoX == -1 || s.piirtoY == -1)
                {
                    continue;
                }
                if (visualizeStars)
                {
                    //Lasketaan magnitudi, ja tulkitaan se suoraan tähden "pikselimääräksi"
                    double distanceToStar = Math.Sqrt(Math.Pow((cameraPoint.X - s.sijainti.X), 2) + Math.Pow((cameraPoint.Y - s.sijainti.Y), 2) + Math.Pow((cameraPoint.Z - s.sijainti.Z), 2));
                    float magnitude = (float)(5 * Math.Log10(distanceToStar / 10) + s.absoluuttinenMagnitudi);
                    if (magnitude > 25)
                    {
                        //Ei piirretä magnitudia 25 himmeämpiä tähtiä
                        continue;
                    }
                    if (magnitude >= 0)
                    {
                        magnitude = 25 - magnitude;
                    }
                    else
                    {
                        magnitude = 25 + Math.Abs(magnitude);
                    }
                    magnitude = 0.5f * magnitude;
                    GraphicsPath p = new GraphicsPath();
                    p.AddEllipse(s.piirtoX, s.piirtoY, magnitude, magnitude);
                    PathGradientBrush pgb = new PathGradientBrush(p);
                    pgb.CenterColor = Color.FromArgb(200, (int)s.väri.X, (int)s.väri.Y, (int)s.väri.Z);
                    Color[] colors = { Color.FromArgb(0, (int)s.väri.X, (int)s.väri.Y, (int)s.väri.Z) };
                    pgb.SurroundColors = colors;
                    pgb.FocusScales = new PointF(0.6f, 0.6f);
                    grafiikka.FillEllipse(pgb, s.piirtoX, s.piirtoY, magnitude, magnitude);
                    pgb.Dispose();
                    p.Dispose();
                }
                else
                {
                    //Tähtien piirtäminen pelkkinä pikseleinä
                    SolidBrush brush = new SolidBrush(Color.FromArgb(255, (int)s.väri.X, (int)s.väri.Y, (int)s.väri.Z));
                    grafiikka.FillRectangle(brush, s.piirtoX, s.piirtoY, 1, 1); //Piirretään jokaisen tädhen sijaintiin piste
                    brush.Dispose();
                }    
            }

            
            esikatselu.Image = kuva;
            grafiikka.Dispose();
        }

        private void suoraLaskenta(List<Star> stars)
        {
            //Lasketaan jokaiseen tähteen kohdistuvat voimat, eli kiihtyvyys
            //TODO: pehmennysparametri
            
            //Lasketaan jokaisen tähden liike vakiokokoisella aika-askelella
            //Tässä käytetty leapfrog menetelmää artikkelista https://arxiv.org/pdf/astro-ph/9710043v1.pdf
            //float timeStep2 = 31556926f; //Yksi vuosi sekunneissa
            //float timeStep = 0.1f;
            foreach (Star s in stars)
            {
                Vector3 halfStepPosition = s.sijainti + 0.5f * timeStep * s.nopeus;
                s.sijainti = halfStepPosition;
                päivitäKiihtyvyys(s, stars);
                s.nopeus = s.nopeus + timeStep * s.kiihtyvyys;
                s.sijainti = s.sijainti + 0.5f * timeStep * s.nopeus;
            }
            globalTime += timeStep;                  
        }

        private void dynaaminenLaskenta(List<Star> kappaleet, int taso)
        {
            //TODO: aikasymmetrisyys näiden kappaleiden liikuttelussa???!!??
            //System.Console.WriteLine(taso);//TODO: poista
            for (int i = 0; i < Math.Pow(2, taso); i++)
            {                
                //Tarkistetaan onko tähtiä joita täytyy siirtää alas (aina voi siirtää alas käsittääkseni)
                bool alempiaOn = false;
                foreach (Star s in kappaleet)
                {
                    double tDyn = tarkkuusParametri * Math.Sqrt(epsilon / Math.Sqrt(Math.Pow(s.kiihtyvyys.X, 2) + Math.Pow(s.kiihtyvyys.Y, 2) + Math.Pow(s.kiihtyvyys.Z, 2)));
                    double minimi = Math.Log(((double)timeStep) / tDyn) / Math.Log(2);
                    //TODO: pitäisikö pyöristää ylös vai alas...
                    int n = (int)Math.Floor(minimi);
                    if (n >= taso + 1)
                    {                        
                        s.taso = taso + 1;
                        alempiaOn = true;
                    }
                }
                if (alempiaOn)
                {
                    //Lähetetään alemmat tasot laskettavaksi, mikäli sellaisia löytyi
                    dynaaminenLaskenta(kappaleet, taso + 1);
                }

                //Lasketaan yksi askel tällä tasolla
                float dynamicTimeStep = (float)(((double)timeStep) / Math.Pow(2, taso));
                foreach (Star s in kappaleet)
                {
                    if (s.taso != taso)
                    {
                        //ei päivitetä muille tasoille kuuluvia kappaleita
                        continue;
                    }
                    Vector3 halfStepPosition = s.sijainti + 0.5f * dynamicTimeStep * s.nopeus;
                    s.sijainti = halfStepPosition;
                    päivitäKiihtyvyys(s, stars);
                    s.nopeus = s.nopeus + dynamicTimeStep * s.kiihtyvyys;
                    s.sijainti = s.sijainti + 0.5f * dynamicTimeStep * s.nopeus;
                }

                //Tarkistetaan onko tähtiä joita täytyy siirtää ylös (paitsi jos taso == 0 tai i==0), mikäli i on parillinen
                if (taso == 0 || i == 0 || i % 2 != 0)
                {
                    //Jokin kriteereistä kappaleen siirtämiseksi ylemmälle tasolle ei täyttynyt joten jatketaan ilman siirtoja
                    continue;
                }
                //Ylemmälle tasolle siirtyviä kappaleita ei tarvitse tallentaa, ne tulee automaattisesti mukaan kun palataan askel ylemmälle tasolle
                //koska niiden sisäinen taso parametri on muutettu niin että ne tullaan laskemaan ylemmällä tasolla
                foreach (Star s in kappaleet)
                {
                    double tDyn = tarkkuusParametri * Math.Sqrt(epsilon / Math.Sqrt(Math.Pow(s.kiihtyvyys.X, 2) + Math.Pow(s.kiihtyvyys.Y, 2) + Math.Pow(s.kiihtyvyys.Z, 2)));
                    double minimi = Math.Log(((double)timeStep) / tDyn) / Math.Log(2);
                    //TODO: pitäisikö pyöristää ylös vai alas...
                    int n = (int)Math.Floor(minimi);
                    if (n < taso)
                    {
                        s.taso = taso - 1;
                    }
                }

            }
        }

        private void päivitäKiihtyvyys(Star a, List<Star> stars)
        {
            Vector3 temp = new Vector3(0, 0, 0);
            foreach (Star b in stars)
            {
                if (b.id == a.id)
                {
                    continue;
                }
                Vector3 distanceVector = b.sijainti - a.sijainti;
                float distance = distanceVector.Length(); //TODO: onko tämä oikein
                double distanceD = (double)distance;
                //Seuraavassa on käytetty pehmennystä artikkelin https://academic.oup.com/mnras/article/314/3/475/969154 pohjalta
                float softenedDenominator = (float)(Math.Pow((Math.Pow(distanceD, 2) + Math.Pow(epsilon, 2)), 1.5));
                temp += b.massa * (distanceVector / softenedDenominator);
            }
            a.kiihtyvyys = G * a.massa * temp;
            //TODO: olettaen että yllä laskettu kiihtyvyys on km/s, muunnetaan se pc/year
            //a.kiihtyvyys = 0.0000010226911586616f * a.kiihtyvyys;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            aloitaSimulaatio();
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            visualizeStars = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            dynamicStepping = checkBox3.Checked;
        }


    }
    /// <summary>
    /// 
    /// </summary>
    public class Star
    {
        public Vector3 sijainti { get; set; }
        public Vector3 nopeus { get; set; }
        public Vector3 kiihtyvyys { get; set; }
        public Vector3 väri { get; set; }
        public float absoluuttinenMagnitudi { get; set; }
        public float massa { get; set; }
        public int taso { get; set; }
        public int id { get; }
        public int piirtoX { get; set; }
        public int piirtoY { get; set; }
        public Star(int id, Vector3 sijainti, Vector3 nopeus, Vector3 kiihtyvyys, Vector3 väri, float absoluuttinenMagnitudi, float massa, int taso, int x, int y)
        {
            this.sijainti = sijainti;
            this.nopeus = nopeus;
            this.kiihtyvyys = kiihtyvyys;
            this.väri = väri;
            this.absoluuttinenMagnitudi = absoluuttinenMagnitudi;
            this.massa = massa;
            this.taso = taso;
            this.id = id;
            this.piirtoX = x;
            this.piirtoY = y;
        }
    }
}
