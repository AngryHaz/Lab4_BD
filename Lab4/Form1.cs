using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace Lab4
{
    public partial class Form1 : Form
    {

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        public static DataTable dataTable = new DataTable();
        public int global_iterator = 0;
        public int lenWindow = 10;  //Размер скользящего окна
        public static int smoothing_MSA = 4; //Сглаживание скользящего среднего
        public static int smoothing_MACD = 24;

        public Form1()
        {
            InitializeComponent();

            //Заполнение таблицы Real-Time
            Thread IniTable = new Thread(new ThreadStart(InitializationDataTable));
            IniTable.Start();

            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 1000;
            timer.Start();
        }

        static void set_columnT(DataTable _dataT, string[] words)
        {
            for (int cs = 0; cs < words.Length; cs++)
            {
                if (cs == 0) //Если это первая колонка, то тип значения = DataTime
                {
                    _dataT.Columns.Add(words[cs], typeof(string));
                }
                else //Иначе тип колонки = double
                {
                    _dataT.Columns.Add(words[cs], typeof(double));
                }
            }

            //Задаем таблице дополнительные колонки для расчета показателей SMA, MACD, OBV
            _dataT.Columns.Add("SMA",   typeof(double));
            _dataT.Columns.Add("MACD",  typeof(double));
            _dataT.Columns.Add("OBV",   typeof(double));

        }

        static void set_valueT(DataTable _dataT, string[] words)
        {
            var nrow = _dataT.NewRow();//Добавляем строку в таблицу
            for (int cs = 0; cs < words.Length; cs++)
            {
                nrow[_dataT.Columns[cs].ColumnName.ToString()] = words[cs]; //По наименованию колонки устаналиваем значение в поле
            }
            nrow["SMA"] = nrow["MACD"] = nrow["OBV"] = 0;
            _dataT.Rows.Add(nrow);
        }

        static void set_value_Indicator(int index_table, string name_Indicator)
        {
            
            double sum_value = 0;
            double sum_value_MACD_bot = 0;
            int bottom_line = 0;
            if (index_table >= smoothing_MSA - 1 && name_Indicator == "SMA")
            {
                //SMA - Скользящее среднее. Формула расчета:
                //https://allfi.biz/Forex/TechnicalAnalysis/Trend-Indicators/prostoe-skolzjashhee-srednee.php
                bottom_line = index_table - (smoothing_MSA - 1);
                for (int i = bottom_line; i <= index_table; i++)
                {
                    sum_value += dataTable.Rows[i].Field<double>("CLOSE");
                }
                dataTable.Rows[index_table].SetField("SMA", sum_value / smoothing_MSA);
            }else if(index_table >= smoothing_MACD - 1 && name_Indicator == "MACD")
            {
                //MACD 
                //https://www.metatrader5.com/ru/terminal/help/indicators/oscillators/macd
                bottom_line = index_table - (smoothing_MACD - 1);
                for (int i = bottom_line; i <= index_table; i++)
                {
                    sum_value += dataTable.Rows[i].Field<double>("CLOSE");
                    if (i == smoothing_MACD / 2 - 1)
                    {
                        sum_value_MACD_bot = sum_value;
                    }
                }
                dataTable.Rows[index_table].SetField("MACD", (sum_value_MACD_bot / (smoothing_MACD / 2)) - (sum_value / smoothing_MACD));
            }
            else if(name_Indicator == "OBV" && index_table > 0)
            {
                
                //OBV 
                //https://www.metatrader5.com/ru/terminal/help/indicators/volume_indicators/obv
                /*Если текущая цена закрытия выше предыдущей, то:

                OBV(i) = OBV(i - 1) + VOLUME(i).

                Если текущая цена закрытия ниже предыдущей, то:

                OBV(i) = OBV(i - 1) - VOLUME(i)

                Если текущая цена закрытия равна предыдущей, то:

                OBV(i) = OBV(i - 1)

                Где:

                OBV(i) — значение индикатора On Balance Volume в текущем периоде;
                OBV(i - 1) — значение индикатора On Balance Volume в предыдущем периоде;
                VOLUME(i) — объем текущего бара. (Кол-во совершенных сделок за время «жизни» бара.)
                */
                double volume = 5; //Надо тянуть из файла
                double previous_price = dataTable.Rows[index_table - 1].Field<double>("CLOSE");
                double current_price = dataTable.Rows[index_table].Field<double>("CLOSE");
                if (current_price > previous_price)
                {
                    dataTable.Rows[index_table].SetField("OBV", previous_price + volume);
                }
                else
                {
                    dataTable.Rows[index_table].SetField("OBV", previous_price - volume);
                }
            }
            else
            {
                return;
            }
        }

        static void set_value_MACD(int index_table)
        {

        }

        static void InitializationDataTable()
        {
            /*  
                Считывание файла в таблицу типа DataGridView
                https://www.bestprog.net/ru/2018/02/17/the-datagridview-control_ru/#q01
                https://coderoad.ru/733556/%D0%9A%D0%B0%D0%BA-%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D1%82%D1%8C-DataGrid-%D0%B2-C
                Возможен вариант асинхронного чтения файла - наиболее близко подходит по сути задачи.
                Считывание строк файла одновременно с их обработкой - симуляция онлайн биржи
            */
            bool createTable = true;
            using (StreamReader sr = new StreamReader(@"C:\Users\AnHaz\Desktop\Универ\BigData\Lab4\Lab4WF\Lab4_input.txt", System.Text.Encoding.Default))
            {
                //[    1    ] [    2    ] [    3    ] [    4    ] [    5    ]
                //| <TIME>  | | <OPEN>  | | <HIGH>  | | <LOW>   | | <CLOSE> |
                string line;
                Console.WriteLine("Началось считывание файла.");
                while ((line = sr.ReadLine()) != null)
                {
                    string[] words = line.Split(new char[] { ';' });
                    if (createTable)
                    {
                        set_columnT(dataTable, words);
                        createTable = false;
                    }
                    else
                    {
                        //DataTable.Rows https://docs.microsoft.com/ru-ru/dotnet/api/system.data.datatable.rows?view=net-5.0
                        set_valueT(dataTable, words);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        //Вызываем отрисовку по такту описанному в timer.Interval смотри Form1()
        void timer_Tick(object sender, EventArgs e)
        {
            if (global_iterator != dataTable.Rows.Count + 1)
            {
                ShowNewPoints(sender, e);
            }
        }

        //Тут отрисовываем 10 рандомных точек
        private void ShowNewPoints(object sender, EventArgs e)
        {

            foreach (var series in ekran.Series) //Чистим прошлые точки
            {
                series.Points.Clear();
            }
           
            //CLOSE
            ekran.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            ekran.Series[0].BorderWidth = 5;
            ekran.Series[0].Color = Color.Aqua;

            //SMA
            ekran.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            ekran.Series[1].BorderWidth = 3;
            ekran.Series[1].Color = Color.OrangeRed;

            //MACD
            ekran.Series[2].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            ekran.Series[2].BorderWidth = 3;
            ekran.Series[2].Color = Color.Green;

            //MACD
            ekran.Series[3].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            ekran.Series[3].BorderWidth = 3;
            ekran.Series[3].Color = Color.Gold;

            const int N = 10;
            int j = 0;

            for (int i = 0; i < lenWindow; i++)
            {
                //Задаем среднее скользящее
                set_value_Indicator(i + global_iterator, "SMA");
                set_value_Indicator(i + global_iterator, "MACD");
                set_value_Indicator(i + global_iterator, "OBV");

                ekran.Series[0].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("CLOSE"));
                ekran.Series[1].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("SMA"));
                ekran.Series[2].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("MACD"));
                ekran.Series[3].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("OBV"));

                j += 1;
            }
            //После того как таймер нарисовал новый график сдвигаем получаемые значения в таблице на на единицу
            global_iterator += 1;

            Axis ax = new Axis();
            ax.Title = "Дата";
            ekran.ChartAreas[0].AxisX = ax;
            Axis ay = new Axis();
            ay.Title = "Цена";
            ekran.ChartAreas[0].AxisY = ay;
            ekran.ChartAreas[0].AxisX.Minimum = 0;
            ekran.ChartAreas[0].AxisX.Maximum = lenWindow - 1;
            ekran.ChartAreas[0].AxisY.Minimum = 100;
            ekran.ChartAreas[0].AxisY.Maximum = 120;

        }
    }
}
