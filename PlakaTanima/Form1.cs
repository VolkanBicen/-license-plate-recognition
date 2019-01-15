using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using openalprnet;

namespace PlakaTanima
{
    public partial class Form1 : Form
    {

        string path = Application.StartupPath.ToString() + "\\plakaRes\\";
        int sayac;
        
        MJPEGStream stream;
        int sayi = 1;
        public string kullaniciAdi { get; set; }
        public string sifre { get; set; }
        public string ip { get; set; }
        public string ip2 { get; set; }
        string plakaKd = "";
        public Form1()
        {
            InitializeComponent();
        }
        public void Form1_Load_1(object sender, EventArgs e)
        {
            stream = new MJPEGStream();
            stream.Source = "http://192.168.1.37:8080/video";//değişecek
            stream.Login = "admin";
            stream.Password = "123";
            stream.NewFrame += stream_NewFrame;
            label7.Text = "Hoşgeldiniz" + " " + kullaniciAdi;
            label8.Text = "IP Adresiniz" + " " + ip;
            timer1.Interval = 3000;
            // timer1.Enabled = true;
            timer1.Start();
        }

        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public Rectangle boundingRectangle(List<Point> points)
        {
            // Add checks here, if necessary, to make sure that points is not null,
            // and that it contains at least one (or perhaps two?) elements

            var minX = points.Min(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxX = points.Max(p => p.X);
            var maxY = points.Max(p => p.Y);

            return new Rectangle(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
        }

        private static Image cropImage(Image img, Rectangle cropArea)
        {
            var bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        public static Bitmap combineImages(List<Image> images)
        {
            //read all images into memory
            Bitmap finalImage = null;

            try
            {
                var width = 0;
                var height = 0;

                foreach (var bmp in images)
                {
                    width += bmp.Width;
                    height = bmp.Height > height ? bmp.Height : height;
                }

                //create a bitmap to hold the combined image
                finalImage = new Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (var g = Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(Color.Black);

                    //go through each image and draw it on the final image
                    var offset = 0;
                    foreach (Bitmap image in images)
                    {
                        g.DrawImage(image,
                                    new Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }


                return finalImage;
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();

                throw ex;
            }
            finally
            {
                //clean up memory
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }
        }

        private void processImageFile(string fileName)
        {


            resetControls();
            textBox1.Text = "";
            var region = "tr";
            String config_file = Path.Combine(AssemblyDirectory, "openalpr.conf");
            String runtime_data_dir = Path.Combine(AssemblyDirectory, "runtime_data");
            using (var alpr = new AlprNet(region, config_file, runtime_data_dir))
            {
                if (!alpr.IsLoaded())
                {
                    lbxPlates.Items.Add("OpenALPR Başlatılamadı");
                    return;
                }
                picOriginal.ImageLocation = fileName;
                picOriginal.Load();

                var results = alpr.Recognize(fileName);

                var images = new List<Image>(results.Plates.Count());
                lbxPlates.Items.Add("\t\t-- Plaka --");
                foreach (var result in results.Plates)
                {
                    var rect = boundingRectangle(result.PlatePoints);
                    var img = Image.FromFile(fileName);
                    var cropped = cropImage(img, rect);
                    images.Add(cropped);


                    foreach (var plate in result.TopNPlates)
                    {
                        string[] harf = new string[plate.Characters.Length];
                        for (int i = 0; i < plate.Characters.Length; i++)
                        {
                            harf[i] = plate.Characters.ToString().Substring(i, 1);

                        }
                        bool a = YasakHarf(harf);
                        bool c = YasakRakam(harf);
                        float oran = plate.OverallConfidence;
                        
                        if ((a == true) && (c == true) && oran > 79)
                        {
                            lbxPlates.Items.Add(string.Format(@" {0} {1}",
                                                      plate.Characters.PadRight(10),
                                                        plate.OverallConfidence.ToString("N1").PadLeft(8)));
                            plakaKd= plate.Characters.ToString().Substring(0, 2);
                            textBox1.Text = plakaKod(plakaKd);
                        }
                    }
                }

                if (images.Any())
                {
                    picLicensePlate.Image = combineImages(images);
                }
            }

            StreamWriter Dosya = File.AppendText("log.txt");
            if (sayac == 0)
            {
                for (int j = 1; j <= lbxPlates.Items.Count - 1; j++)
                {
                    Dosya.WriteLine(DateTime.Now.ToString() + "\t" + lbxPlates.Items[j].ToString()
                       + "\t" + textBox1.Text);
                }
                sayac = 1;
            }
            Dosya.Close();
        }
        public string plakaKod(string plaka)
        {
            string sehirKod = "";

            List<string> trplakakodlar = new List<string>();
            for (int kod = 1; kod <= 81; kod++)
            {
                trplakakodlar.Add(string.Format("{0:00}", kod));

            }
            foreach (string b in trplakakodlar)
            {

                if (plaka == b )
                {
                    string[] sehir = { "Adana", "Adıyaman", "Afyon", "Ağrı", "Amasya", "Ankara", "Antalya",
                            "Artvin", "Aydın", "Balıkesir", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur",
                            "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Edirne", "Elazığ",
                            "Erzincan", "Erzurum'da", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkari",
                            "Hatay", "Isparta", "İçel", "İstanbul", "İzmir", "Kars", "Kastamonu", "Kayseri",
                            "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
                            "Kahramanmaraş", "Mardin", "Muğla", "MUS", "Nevşehir", "Niğde", "Ordu", "Rize",
                            "Sakarya", "Samsun", "Siirt", "Sinop", "Sivas", "Tekirdağ", "Tokat", "Trabzon",
                            "Tunceli", "Şanlıurfa", "Uşak", "Van", "Yozgat", "Zonguldak", "Aksaray",
                            "Bayburt", "Karaman", "Kırıkkale", "batman", "Şırnak", "Bartın", "Ardahan", "Iğdır",
                            "Yalova", "Karabük", "Kilis", "Osmaniye", "Düzce" };
                    sehirKod = (sehir[trplakakodlar.IndexOf(plaka)]);
                }
            }

            return sehirKod;
        }
        public bool YasakRakam(string[] dizi)
        {
            Console.WriteLine(dizi.Length);
            int sayac = 0;
            string[] rakam = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string[] harf = { "A", "B", "C", "D", "E", "D", "F", "G", "H", "I",
            "J", "K", "L", "M", "N", "O", "P", "R", "S", "T","U","V","Y","Z"};

            for (int i = 0; i < rakam.Length; i++)
            {

                if (string.Compare(dizi[2], rakam[i]) == 0)
                {
                    sayac++;
                }
                if (string.Compare(dizi[3], rakam[i]) == 0)
                {
                    sayac++;
                }

            }
            for (int i = 0; i < harf.Length; i++)
            {
                if (string.Compare(dizi[0], harf[i]) == 0)
                {
                    sayac++;
                }
                if (string.Compare(dizi[1], harf[i]) == 0)
                {
                    sayac++;
                }
                if (string.Compare(dizi[5], harf[i]) == 0)
                {
                    sayac++;
                }
                if (string.Compare(dizi[6], harf[i]) == 0)
                {
                    sayac++;
                }
            }
           

            if (sayac == 0)
            {
                return true;
            }
            else return false;

        }
        //****************************************************
        public bool YasakHarf(string[] dizi)
        {
            int sayac = 0;
            string[] harftr = { "Ç", "Ş", "Ü", "İ", "Ö", "W", "X", "Q" };
            for (int i = 0; i < harftr.Length; i++)
            {
                for (int j = 0; j < dizi.Length; j++)
                {
                    if (string.Compare(dizi[j], harftr[i]) == 0)
                    {
                        sayac = sayac + 1;

                    }
                }
            }

            if (sayac == 0)
            {
                return true;
            }
            else return false;

        }
        //****************************************************************************
        private void resetControls()
        {
            picOriginal.Image = null;
            picLicensePlate.Image = null;
            lbxPlates.Items.Clear();
        }
        public void photo()
        {
            if (picOriginal.Image != null)
            {
                sayac = 0;
                Bitmap bt = new Bitmap(picOriginal.Image);
                Random rastgele = new Random();
                int sayi = rastgele.Next();
                string kyt = "";
                string Klasor = path + sayi;
                Directory.CreateDirectory(Klasor);
                kyt = Klasor + "\\" + "a.jpg";
                bt.Save(kyt);
                processImageFile(kyt);
            }

        }
        public void timer1_Tick(object sender, EventArgs e)
        {

                sayi++;
                if (sayi%19==0)
                {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                  
                }
                catch (Exception)
                {
                   
                  

                }
            }
            
            stream.Start();
            photo();

        }
        void stream_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmp = (Bitmap)eventArgs.Frame.Clone();
            picOriginal.Image = bmp;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    timer1.Stop();
                    resetControls();
                    stream.Stop();
                    Application.Exit();
                }
            }
            catch (Exception)
            {
                resetControls();
                stream.Stop();
                Application.Exit();

            }


        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            Giris giris = new Giris();
            giris.Show();
        }
    }
}