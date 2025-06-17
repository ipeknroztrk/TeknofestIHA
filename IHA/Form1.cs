using System;
using System.Windows.Forms;
using System.Text.Json;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IHA
{
    public partial class Form1 : Form
    {
        bool dragging = false;
        Point dragCursorPoint;
        Point dragFormPoint;



        public Form1()
        {
            InitializeComponent();
            StartTcpClient();
            this.Load += Form1_Load;
           
            panelHeader.MouseDown += panelHeader_MouseDown;
            panelHeader.MouseMove += panelHeader_MouseMove;
            panelHeader.MouseUp += panelHeader_MouseUp;
            // Üst sabit bar
            panelHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Kapat - simge - büyüt tuşları (panelHeader içindeki label'lar)
            label6.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Radar kısmı sola sabit ve dikeyde esnek
            panelRadar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            // Kamera (görüntü) kısmı sağa sabit ve dikeyde esnek
            pictureBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

            // Sol alt: Veri Paneli
            groupBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            // Orta alt: Diğer veriler
            groupBox2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            // Sağ alt: Kilitlenme Paneli
            groupBox2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;


            // g paneli (tarih/saat kutusu)
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;


            // Logo paneli sabit kalsın (üst sol)
            pictureBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        }


        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void panelHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void panelHeader_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void StartTcpClient()
        {
            new Thread(() =>
            {
                try
                {
                    TcpClient client = new TcpClient("127.0.0.1", 5050);

                    StreamReader reader = new StreamReader(client.GetStream());

                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        Console.WriteLine("Veri alındı: " + line);

                        MissionData veri = null;
                        try
                        {
                            veri = JsonSerializer.Deserialize<MissionData>(line);
                        }
                        catch (JsonException jsonEx)
                        {
                            Console.WriteLine("JSON çözümlenemedi: " + jsonEx.Message);
                            continue;
                        }

                        if (veri != null)
                        {
                            Invoke(new Action(() =>
                            {
                                lblFlightMode.Text = veri.ucus_modu;
                                lblBattery.Text = $"{veri.iha_batarya:0.00} %";
                                lblBatteryPercent.Text = $"{veri.iha_batarya:0} %";

                                lblAltitude.Text = $"{veri.iha_irtifa:0.0} m";
                                lblPitch.Text = $"{veri.iha_dikilme:0.0}°";
                                lblYaw.Text = $"{veri.iha_yonelme:0.0}°";
                                lblRoll.Text = $"{veri.iha_yatis:0.0}°";
                                lblGroundSpeed.Text = $"{veri.iha_hiz:0.00} m/s";
                                lblLatitude.Text = $"{veri.iha_enlem:0.000000}";
                                lblLongitude.Text = $"{veri.iha_boylam:0.000000}";

                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("TCP bağlantı hatası: " + ex.Message);
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timerheat.Interval = 600000;
            timerheat.Start();
            panelRadar.Paint += panelRadar_Paint;


            lblhour.Text = DateTime.Now.ToShortTimeString();
            lbldate.Text = DateTime.Now.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

        }




        private void timer1_Tick(object sender, EventArgs e)
        {
        }




        private void panelRadar_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(30, 30, 30)); // Arka plan koyu

            int centerX = panelRadar.Width / 2;
            int centerY = panelRadar.Height / 2;

            // Daha belirgin çizgiler için opaklık ve kalınlık artırıldı
            Pen gridPen = new Pen(Color.FromArgb(150, 79, 142, 220), 1.5f); // Radar halkaları
            Pen axisPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.8f); // Eksen çizgisi
            Font font = new Font("Segoe UI", 8, FontStyle.Bold);

            for (int r = 50; r <= 200; r += 50)
                g.DrawEllipse(gridPen, centerX - r, centerY - r, r * 2, r * 2);

            g.DrawLine(axisPen, centerX, 0, centerX, panelRadar.Height);
            g.DrawLine(axisPen, 0, centerY, panelRadar.Width, centerY);

            DrawTarget(g, centerX, centerY, "KIRLANGIÇ", 145, Brushes.Cyan, font);
            DrawTarget(g, centerX + 80, centerY - 60, "SİHA-1", 120, Brushes.Red, font);
            DrawTarget(g, centerX - 90, centerY + 40, "SİHA-2", 100, Brushes.OrangeRed, font);

            DrawDistanceLine(g, centerX, centerY, centerX + 80, centerY - 60, font);
            DrawDistanceLine(g, centerX, centerY, centerX - 90, centerY + 40, font);
        }

        private void DrawTarget(Graphics g, int x, int y, string name, int irtifa, Brush color, Font font)
        {
            GraphicsPath planePath = new GraphicsPath();

            // Gövde
            planePath.AddRectangle(new Rectangle(x - 2, y - 10, 4, 20));

            // Kanatlar
            planePath.AddPolygon(new Point[]
            {
        new Point(x - 10, y),
        new Point(x, y - 4),
        new Point(x + 10, y),
        new Point(x, y + 4)
            });

            // Kuyruk
            planePath.AddPolygon(new Point[]
            {
        new Point(x - 3, y - 10),
        new Point(x + 3, y - 10),
        new Point(x, y - 15)
            });

            g.FillPath(color, planePath);
            g.DrawString($"{name}\nİrtifa: {irtifa}m", font, Brushes.White, x + 10, y - 10);
        }

        private void DrawDistanceLine(Graphics g, int x1, int y1, int x2, int y2, Font font)
        {
            Pen dashed = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dash };
            g.DrawLine(dashed, x1, y1, x2, y2);

            double dist = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            int midX = (x1 + x2) / 2;
            int midY = (y1 + y2) / 2;

            g.DrawString($"{dist:0} ", font, Brushes.LightGray, midX + 5, midY);
        }


       

        private void label6_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lblMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private bool isMaximized = false;

        private void lblMaximize_Click(object sender, EventArgs e)
        {
            if (isMaximized)
            {
                this.WindowState = FormWindowState.Normal;
                isMaximized = false;
                lblMaximize.Text = "🗖"; 
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
                isMaximized = true;
                lblMaximize.Text = "🗗"; 
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void lblFlightMode_Click(object sender, EventArgs e)
        {

        }
    }
}


public class MissionData
{
    public string ucus_modu { get; set; }
    public int takim_numarasi { get; set; }
    public float iha_enlem { get; set; }
    public float iha_boylam { get; set; }
    public float iha_irtifa { get; set; }
    public float iha_dikilme { get; set; }
    public float iha_yonelme { get; set; }
    public float iha_yatis { get; set; }
    public float iha_hiz { get; set; }
    public float iha_batarya { get; set; }
    public int iha_otonom { get; set; }
    public int iha_kilitlenme { get; set; }
    public int hedef_merkez_X { get; set; }
    public int hedef_merkez_Y { get; set; }
    public int hedef_genislik { get; set; }
    public int hedef_yukseklik { get; set; }
    public GpsSaati gps_saati { get; set; }
}

public class GpsSaati
{
    public int saat { get; set; }
    public int dakika { get; set; }
    public int saniye { get; set; }
    public int milisaniye { get; set; }
}
