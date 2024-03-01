using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Youtube_Live_Chat_Reformat
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Counter : Window
    {
        private Timer _timer;
        private MainWindow window;
        public Counter(MainWindow mainWindow)
        {
            this.window = mainWindow;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(()=>{
                var _liteDatabase = new LiteDatabase(window.liteDBString);
                var chat = _liteDatabase.GetCollection<ChatData>("chat");
                var filters = filter.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                var list = chat.FindAll().ToList();
                if (filters.Count > 0)
                {
                    foreach (var filter in filters.ToList())
                    {
                        if (filter.Contains("-"))
                        {
                            var predict = filter.Split('-');
                            if (predict.Length > 1)
                            {
                                if (!int.TryParse(predict[0], out int min))
                                {
                                    continue;
                                }
                                if (!int.TryParse(predict[1], out int max))
                                {
                                    continue;
                                }
                                filters.Remove(filter);
                                filters.AddRange(Enumerable.Range(min, max - min + 1).Select(x => x.ToString()));
                            }
                        }
                    }
                    var result = list.Where((x) =>
                    {
                        foreach(var filter in filters.ToList())
                        {
                            if (int.TryParse(filter, out int compare))
                            {
                                if(int.TryParse(x.Comment, out int comment))
                                {
                                    if(compare == comment)
                                    {
                                        return true;
                                    }
                                }
                            }
                            else if (x.Comment?.Contains(filter) ?? false)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                    if (showOnce.IsChecked ?? false)
                    {
                        result = result.GroupBy(x => x.User).Select(y => y.First());
                    }
                    grid.ItemsSource = result;
                    Count.Content = result.Count();
                    List<CounterData> counters = new List<CounterData>();
                    foreach(var filter in filters)
                    {
                        if(int.TryParse(filter, out int num))
                        {
                            counters.Add(new CounterData
                            {
                                Count = result.Count(x => x.Comment == filter),
                                Keyword = filter,
                            });
                        }
                        else
                        {
                            counters.Add(new CounterData
                            {
                                Count = result.Count(x => x.Comment.StartsWith(filter)),
                                Keyword = filter,
                            });
                        }
                    }
                    counter.ItemsSource = counters;
                }
                else
                {
                    grid.ItemsSource = list;
                    Count.Content = list.Count();
                    List<CounterData> counters = new List<CounterData>();
                    counter.ItemsSource = counters;
                }
                _liteDatabase.Dispose();
            });
            
        }
    }
}
