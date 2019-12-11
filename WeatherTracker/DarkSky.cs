using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using ZedGraph;
using MathNet.Numerics.Interpolation;

namespace WeatherTracker
{
    public class DarkSkyData
    {
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public List<DarkSkyDay> Data;
        public CubicSpline WindSpeed;
        public string Key;
        public string DataFile;

        public DarkSkyData()
        {
            DataFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeatherTracker", "data.json");
 
        }


        public DarkSkyData(string key, string latitude, string longitude)
        {
            Key = key;
            Latitude = latitude;
            Longitude = longitude;
            Data = new List<DarkSkyDay>();
            DataFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeatherTracker", "data.json");
        }
        public void Load()
        {
            string json = File.ReadAllText(DataFile);
            DarkSkyData dsd = JsonConvert.DeserializeObject<DarkSkyData>(json);

            this.Latitude = dsd.Latitude;
            this.Longitude = dsd.Longitude;
            this.Key = dsd.Key;
            this.DataFile = dsd.DataFile;
            this.Data = dsd.Data;
        }
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            string dir = Path.GetDirectoryName(DataFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (StreamWriter sw = new StreamWriter(DataFile))
            {
                sw.Write(json);
            }
        }
        public DarkSkyDay AddDay(DateTime day)
        {
            string response;
            using (var client = new WebClient()) // WebClient class inherits IDisposable
            {
                string url = GetUrl(day);
                response = client.DownloadString(url);
            }
            System.Diagnostics.Debug.WriteLine(response);
            DarkSkyDay dsd = JsonConvert.DeserializeObject<DarkSkyDay>(response);
            dsd.Date = day;
            Data.Add(dsd);

            UpdateCurves();


            return dsd;
        }
        public void UpdateCurves()
        {
            //CubicSpline

        }
        public System.Windows.Forms.DataVisualization.Charting.Chart GetChart(System.Windows.Forms.DataVisualization.Charting.Chart cc)
        {
            cc.Series.Clear();
            cc.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;

            Series s = new Series();

            s.ChartType = SeriesChartType.Line;
            s.MarkerStyle = MarkerStyle.Circle;

            foreach (var d in Data)
            {
                DateTime current = d.Date.Date;
                int i = 0;
                foreach (Datum h in d.hourly.data)
                {
                    //DateTime hour = current.Add(new TimeSpan(i, 0, 0));
                    DateTime hour = UnixTimestampToDateTime(h.time);
                    hour = TimeZone.CurrentTimeZone.ToLocalTime(hour);

                    System.Windows.Forms.DataVisualization.Charting.DataPoint dp = new System.Windows.Forms.DataVisualization.Charting.DataPoint();
                    string dir = DegreesToCardinalDetailed(h.windBearing);
                    dp.ToolTip = dir + "\n" + hour.ToShortDateString() + "\n" + hour.ToShortTimeString();
                    dp.SetValueXY(hour, h.windSpeed);
                    s.Points.Add(dp);
                }
            }
            cc.Series.Add(s);
            return cc;
        }
        public ZedGraph.ZedGraphControl GetZedGraph(ZedGraphControl zgc)
        {

            GraphPane pane = zgc.GraphPane;
            pane.XAxis.Type = ZedGraph.AxisType.Date;
            pane.XAxis.Scale.Format = "MM/dd/yyy";
            pane.XAxis.Scale.FontSpec.Angle = -90;
            pane.XAxis.Scale.MajorUnit = DateUnit.Day;
            pane.XAxis.Scale.MajorStep = 7;
            pane.XAxis.Scale.MinorUnit = DateUnit.Day;
            pane.XAxis.Scale.MinorStep = 1;
            
            pane.CurveList.Clear();

            PointPairList ppl = new PointPairList();

            foreach (var d in Data)
            {
                DateTime current = d.Date.Date;
                int i = 0;
                foreach (Datum h in d.hourly.data)
                {
                    //DateTime hour = current.Add(new TimeSpan(i, 0, 0));
                    DateTime hour = UnixTimestampToDateTime(h.time);
                    hour = TimeZone.CurrentTimeZone.ToLocalTime(hour);

                    string dir = DegreesToCardinalDetailed(h.windBearing);
                    string tip = dir + ", " + h.windSpeed.ToString() + "\n" + hour.ToShortDateString() + "\n" + hour.ToShortTimeString();
                    
                    PointPair pp = new PointPair(new XDate(hour), h.windSpeed);
                    pp.Tag = tip;
                    
                    ppl.Add(pp);
                }
            }

            LineItem curve = pane.AddCurve("wind", ppl, System.Drawing.Color.Blue, SymbolType.Triangle);
            
            zgc.AxisChange();
            zgc.Invalidate();
            return zgc;
        }
        public string GetUrl(DateTime dt)
        {
            string d = dt.ToString("yyyy-MM-ddTHH:mm:ss");
            string url = "https://api.darksky.net/forecast/" +
                Key + "/" +
            Latitude + "," +
            Longitude + "," +
            d;
            return url;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;
            return (double)unixTimeStampInTicks / TimeSpan.TicksPerSecond;
        }
        public static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
        }
        public static string DegreesToCardinalDetailed(double degrees)
        {
            string[] caridnals = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" };
            return caridnals[(int)Math.Round(((double)degrees * 10 % 3600) / 225)];
        }
    }
}
