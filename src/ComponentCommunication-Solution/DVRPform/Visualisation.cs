using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UCCTaskSolver;

namespace FileManager
{
    public partial class Visualisation : Form
    {
        Bitmap bitmap;
        List<Color> names = new List<Color>(){
            Color.Aqua,
            Color.DarkOrange,
            Color.Maroon,
            Color.Green,
            Color.DeepPink,
            Color.Brown,
            Color.Gold,
            Color.Salmon,
            Color.DarkBlue,
            Color.DarkViolet,
            Color.SkyBlue,
            Color.Olive,
            Color.Black
        };
        bool is_show = false;
        int is_show_index = -1;
        Random m_random = new Random();
        int m_margin = 50;
        private DVRPDescription m_dvrpDescription;
        int m_maxX = int.MinValue, m_maxY = int.MinValue, m_minX = int.MaxValue, m_minY = int.MaxValue;
        Point[] m_clientsCoordinates;
        Point m_depotCoordinates;

        public Visualisation(DVRPDescription _dvrpDescription, int[][] _permutations)
        {
            InitializeComponent();

            m_dvrpDescription = _dvrpDescription;
            m_clientsCoordinates = new Point[_dvrpDescription.clients.Count];
            bitmap = new Bitmap(panel1.Width, panel1.Height);
            foreach (Client client in _dvrpDescription.clients)
            {
                if ((int)client.coordinate.X < m_minX)
                    m_minX = (int)client.coordinate.X;
                if ((int)client.coordinate.X > m_maxX)
                    m_maxX = (int)client.coordinate.X;
                if ((int)client.coordinate.Y < m_minY)
                    m_minY = (int)client.coordinate.Y;
                if ((int)client.coordinate.Y > m_maxY)
                    m_maxY = (int)client.coordinate.Y;
            }

            drawDepot(m_dvrpDescription);
            drawEllipses(m_dvrpDescription);
            drawLines(_permutations);
        }

        private void drawLines(int[][] _permutations)
        {
            Color m_color;
            Label m_tmp_label;
            int index;
            SolidBrush[] brushes = new SolidBrush[_permutations.Length];

            for (int i = 0; i < brushes.Length; i++)
            {
                index = m_random.Next(names.Count);
                m_color = names[index];
                names.RemoveAt(index);
                brushes[i] = new System.Drawing.SolidBrush(m_color);
                m_tmp_label = new Label() { Text = "Vehicle nr " + i.ToString(), Anchor = AnchorStyles.Left, AutoSize = true };
                m_tmp_label.ForeColor = m_color;
                m_tmp_label.BackColor = Color.Transparent;
                flowLayoutPanel1.Controls.Add(m_tmp_label);
            }



            using (Graphics g = Graphics.FromImage(bitmap))
            {
                for (int i = 0; i < _permutations.Length; i++)
                {
                    for (int j = 0; j < _permutations[i].Length; j++)
                    {
                        if (j == 0)
                            g.DrawLine(new Pen(brushes[i]), m_depotCoordinates, m_clientsCoordinates[_permutations[i][j]]);
                        else
                        {
                            g.DrawLine(new Pen(brushes[i]), m_clientsCoordinates[_permutations[i][j - 1]], m_clientsCoordinates[_permutations[i][j]]);
                            if (j == _permutations[i].Length - 1)
                                g.DrawLine(new Pen(brushes[i]), m_depotCoordinates, m_clientsCoordinates[_permutations[i][j]]);
                        }

                    }
                }
                panel1.Refresh();
            }
            foreach (var b in brushes)
                b.Dispose();


        }

        private void drawDepot(DVRPDescription _dvrpDescription)
        {
            SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Blue);

            int x = (int)_dvrpDescription.coordinateDepot.X + Math.Abs(m_minX) + m_margin;
            int y = (int)_dvrpDescription.coordinateDepot.Y + Math.Abs(m_minY) + 30;
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(myBrush, new Rectangle(x, y, 10, 10));
            }
            m_depotCoordinates = new Point(x + 5, y + 5);
            myBrush.Dispose();
            panel1.Refresh();
        }

        private void drawEllipses(DVRPDescription _dvrpDescription)
        {
            int x, y;
            SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                for (int i = 0; i < _dvrpDescription.clients.Count; i++)
                {
                    x = (int)_dvrpDescription.clients[i].coordinate.X + Math.Abs(m_minX) + m_margin;
                    y = (int)_dvrpDescription.clients[i].coordinate.Y + Math.Abs(m_minY) + 30;
                    m_clientsCoordinates[i] = new Point(x + 5, y + 5);
                    g.FillEllipse(myBrush, new Rectangle(x, y, 13, 13));
                    g.DrawString(i.ToString(), new Font("Arial", 9), new SolidBrush(Color.White), new PointF(x, y));
                }
            }
            myBrush.Dispose();
            panel1.Refresh();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(bitmap, Point.Empty);
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            int prev_index = is_show_index;
            for (int i = 0; i < m_clientsCoordinates.Length; i++)
            {
                if ((e.X >= m_clientsCoordinates[i].X && e.X <= m_clientsCoordinates[i].X + 20)
                    &&
                    (e.Y >= m_clientsCoordinates[i].Y && e.Y <= m_clientsCoordinates[i].Y + 20))
                {
                    is_show = true;
                    is_show_index = i;
                    break;
                }
                else
                    is_show_index = -1;
            }

            if (is_show_index < 0)
            {
                is_show = false;
                flowLayoutPanel2.Controls.Clear();
                is_show_index = -1;
            }

            if (is_show && prev_index != is_show_index)
            {
                flowLayoutPanel2.Controls.Clear();
                flowLayoutPanel2.Controls.Add(new Label { Text = "Client" });
                flowLayoutPanel2.Controls.Add(new Label { Text = "X: " + m_dvrpDescription.clients[is_show_index].coordinate.X });
                flowLayoutPanel2.Controls.Add(new Label { Text = "Y: " + m_dvrpDescription.clients[is_show_index].coordinate.Y });
                flowLayoutPanel2.Controls.Add(new Label { Text = "Number: " + is_show_index.ToString() });
                flowLayoutPanel2.Controls.Add(new Label { Text = "Available time: " + m_dvrpDescription.clients[is_show_index].availableTime });
                flowLayoutPanel2.Controls.Add(new Label { Text = "Demand: " + m_dvrpDescription.clients[is_show_index].demand });
                flowLayoutPanel2.Controls.Add(new Label { Text = "Duration time: " + m_dvrpDescription.clients[is_show_index].durationTime });

            }


        }


    }

}
