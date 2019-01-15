using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlakaTanima
{
    public partial class Giris : Form
    {
        public Giris()
        {
            InitializeComponent();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 form1 = new Form1();
            form1.Show();
        }

        public void button1_Click(object sender, EventArgs e)
        {
            if (txtip.Text=="")
            {
                MessageBox.Show("Ip adresini boş bırakmayınız. ");
                return;
            }

            this.Hide();
            Form1 form1 = new Form1();
            form1.kullaniciAdi = txtkul.Text;
            form1.sifre = txtsifre.Text;
            form1.ip = txtip.Text;
            form1.Show();
        }

        private void Giris_Load(object sender, EventArgs e)
        {

        }
    }
}
