﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Timers;
using System.Windows;

namespace Youtube_Live_Chat_Reformat
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Counter : Window
    {
        private readonly MainWindow window;
        private bool pause;
        private List<CounterData> counters = new List<CounterData>();
        private List<Chart> charts = new List<Chart>();
        private readonly Thread t;
        private readonly List<ChatData> chatDatas = new List<ChatData>();
        public Counter(MainWindow mainWindow)
        {
            window = mainWindow;
            InitializeComponent();
            LiteDatabase _liteDatabase = new LiteDatabase(window.liteDBString);
            var chat = _liteDatabase.GetCollection<ChatData>("chat");
            chatDatas.AddRange(chat.FindAll());
            t = new Thread(() =>
            {
                do
                {
                    Tick();
                }
                while (true);
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            t.IsBackground = true;
            t.Start();
        }

        internal void AddMessage(ChatData message)
        {
            chatDatas.Add(message);
        }

        internal void Reset()
        {
            chatDatas.Clear();
            LiteDatabase _liteDatabase = new LiteDatabase(window.liteDBString);
            var chat = _liteDatabase.GetCollection<ChatData>("chat");
            chatDatas.AddRange(chat.FindAll());
        }

        private void Tick()
        {
            if (pause)
            {
                return;
            }
            counters = new List<CounterData>();
            IQueryable<ChatData> list = chatDatas.AsQueryable().Where(x => x.Comment != null && x.User != null);
            List<string> filters = new List<string>();
            Dispatcher.Invoke(() =>
            {
                filters = filter.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            });
            List<int> numFilters = new List<int>();
            List<string> strFilters = new List<string>();
            if (filters.Count > 0)
            {
                foreach (string filter in filters)
                {
                    if (filter.Contains("-"))
                    {
                        string[] predict = filter.Split('-');
                        if (predict.Length > 1)
                        {
                            if (!int.TryParse(predict[0], out int min))
                            {
                                goto Label;
                            }
                            if (!int.TryParse(predict[1], out int max))
                            {
                                goto Label;
                            }
                            numFilters.AddRange(Enumerable.Range(min, max - min + 1));
                            continue;
                        }
                    }
                Label:
                    if (int.TryParse(filter, out int num))
                    {
                        numFilters.Add(num);
                    }
                    else
                    {
                        strFilters.Add(filter);
                    }
                }
                var result = list;
                Dispatcher.Invoke(() =>
                {
                    if (showOnce.IsChecked ?? false)
                    {
                        result = result.GroupBy(x => x.User).Select(x => x.First());
                    }
                });
                result = result.Where((x) => QueryFilter(x, strFilters, numFilters));
                Dispatcher.Invoke(() =>
                {
                    grid.ItemsSource = result;
                    Count.Content = result.Count();
                    foreach (string filter in strFilters)
                    {
                        counters.Add(new CounterData
                        {
                            Count = result.Count(x => x.Comment.StartsWith(filter)),
                            Keyword = filter,
                        });
                    }
                    foreach (int filter in numFilters)
                    {
                        counters.Add(new CounterData
                        {
                            Count = result.Count(x => x.Comment == filter.ToString()),
                            Keyword = filter.ToString(),
                        });
                    }
                    counter.ItemsSource = counters;
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    var result = list;
                    if (showOnce.IsChecked ?? false)
                    {
                        result = result.GroupBy(x => x.User).Select(x => x.First());
                    }
                    grid.ItemsSource = result;
                    Count.Content = result.Count();
                    counter.ItemsSource = counters;
                });
            }
            Dispatcher.Invoke(() => SCAmount.Content = list.Where(x => x.SCAmount > 0).Count());
            foreach (var chart in charts)
            {
                chart.UpdateChart(counters);
            }
            Thread.Sleep(1000);
        }

        private bool QueryFilter(ChatData x, IEnumerable<string> strFilters, IEnumerable<int> numFilters)
        {
            bool match = false;
            if (int.TryParse(x.Comment, out _) && numFilters.Count() > 0)
            {
                match = numFilters.Any(y => x.Comment == y.ToString());
            }
            if (!match && strFilters.Count() > 0)
            {
                match = strFilters.Any(y => ContainsCaseInsensitive(x.Comment, y));
            }
            return match;
        }

        public bool ContainsCaseInsensitive(string source, string substring)
        {
            return source?.IndexOf(substring, StringComparison.OrdinalIgnoreCase) > -1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LiteDatabase _liteDatabase = new LiteDatabase(window.liteDBString);
            ILiteCollection<ChatData> chat = _liteDatabase.GetCollection<ChatData>("chat");
            _ = chat.DeleteAll();
            _liteDatabase.Dispose();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            pause = !pause;
            if (pause)
            {
                pauseBtn.Content = "Start";
            }
            else
            {
                //clean history
                LiteDatabase _liteDatabase = new LiteDatabase(window.liteDBString);
                ILiteCollection<ChatData> chat = _liteDatabase.GetCollection<ChatData>("chat");
                _ = chat.DeleteAll();
                _liteDatabase.Dispose();
                pauseBtn.Content = "Stop";
            }
        }

        private void Pie_Chart_Click(object sender, RoutedEventArgs e)
        {
            Chart chart = new Chart("pie");
            chart.Show();
            chart.Closing += Chart_Closing;
            charts.Add(chart);
        }

        private void Chart_Closing(object sender, CancelEventArgs e)
        {
            charts.Remove(sender as Chart);
        }

        private void Line_Chart_Click(object sender, RoutedEventArgs e)
        {
            Chart chart = new Chart("line");
            chart.Show();
            chart.Closing += Chart_Closing;
            charts.Add(chart);
        }
    }
}
