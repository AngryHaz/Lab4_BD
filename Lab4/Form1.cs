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
        
        //SMA - Скользящее среднее. Формула расчета:
        //https://allfi.biz/Forex/TechnicalAnalysis/Trend-Indicators/prostoe-skolzjashhee-srednee.php

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
            _dataT.Rows.Add(nrow);
        }
        static void set_valueSMA(int index_table)
        {
            int smoothing = 4; //Сглаживание

            if (index_table < smoothing - 1)
            {
                dataTable.Rows[index_table].SetField("SMA", 0);
                return;
            }
            else
            {
                double sum_value = 0;
                //double SMA = 0;
                int bottom_line = index_table - (smoothing - 1);
                for (int i = bottom_line; i <= index_table; i++)
                {
                    sum_value += dataTable.Rows[i].Field<double>("OPEN");
                }
                dataTable.Rows[index_table].SetField("SMA", sum_value / smoothing);
            }
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
           
            //OPEN
            //ekran.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            
            //CLOSE
            ekran.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            ekran.Series[0].BorderWidth = 3;
            ekran.Series[0].Color = Color.Aqua;

            //SMA
            ekran.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            ekran.Series[1].BorderWidth = 3;
            ekran.Series[0].Color = Color.OrangeRed;

            const int N = 10;
            int j = 0;

            for (int i = 0; i < lenWindow; i++)
            {
                //Задаем среднее скользящее
                set_valueSMA(i + global_iterator);

                ekran.Series[0].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("OPEN"));
                ekran.Series[1].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("SMA"));
                //ekran.Series[1].Points.AddXY(j, dataTable.Rows[i + global_iterator].Field<double>("CLOSE"));

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
