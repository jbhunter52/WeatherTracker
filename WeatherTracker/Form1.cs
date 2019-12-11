using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace WeatherTracker
{
    public partial class WeatherTracker : Form
    {
        public Timer timer;
        public DarkSkyData DS;
        public System.Windows.Forms.DataVisualization.Charting.Chart mschart;
        public ZedGraph.ZedGraphControl zedGraph;
        public string Key = "9bcfea8cb06a73edcc86bbff631e5d69";
        public WeatherTracker()
        {
            InitializeComponent();
            textBoxLat.Text = "37.997786";
            textBoxLong.Text = "-89.513871";
            dateTimePickerStart.Value = DateTime.Now.Date.Subtract(new TimeSpan(10, 0, 0, 0));


            //mschart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            //mschart.Dock = DockStyle.Fill;
            //mschart.ChartAreas.Add("wind");
            //splitContainer2.Panel1.Controls.Add(mschart);

            zedGraph = new ZedGraphControl();
            zedGraph.Dock = DockStyle.Fill;
            splitContainer2.Panel1.Controls.Add(zedGraph);
            
            
            
        }

        void timer_Tick(object sender, EventArgs e)
        {
            //Get last day in collected data
            DateTime last = DS.Data[DS.Data.Count - 1].Date;

            //Get 2.5 days forward
            DateTime noonNext = last.Add(new TimeSpan(2,12,0,0));
            if (DateTime.Now > noonNext)
            {
                DateTime newDay = last.Add(new TimeSpan(1,0,0,0));
                DS.AddDay(newDay);
                //mschart = DS.GetChart(mschart);
                zedGraph = DS.GetZedGraph(zedGraph);
                DS.Save();
                richTextBox1.AppendText(DateTime.Now.ToString() + ", Added day, " + newDay.ToShortDateString());
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            Initialize();
        }
        private void Initialize()
        {
            DS = new DarkSkyData(Key, textBoxLat.Text, textBoxLong.Text);

            bool run = true;
            DateTime now = DateTime.Now;
            DateTime current = dateTimePickerStart.Value;
            while (run)
            {
                if (current.Date.Equals(now.Date))
                {
                    run = false;
                    continue;
                }

                DS.AddDay(current);
                current = current.Add(new TimeSpan(1, 0, 0, 0));
                System.Threading.Thread.Sleep(100);
            }

            //mschart = DS.GetChart(mschart);
            zedGraph = DS.GetZedGraph(zedGraph);
            DS.Save();
            timer = new Timer();
            double minutes = .1;
            timer.Interval = (int)(minutes * 60 * 1000);
            timer.Tick += timer_Tick;
            timer.Enabled = true;
        }


        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DS = new DarkSkyData();
            DS.Load();
            //mschart = DS.GetChart(mschart);
            zedGraph = DS.GetZedGraph(zedGraph);
            textBoxLat.Text = DS.Latitude;
            textBoxLong.Text = DS.Longitude;

        }



    }
}
