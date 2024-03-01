using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;

namespace Youtube_Live_Chat_Reformat
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Counter : Window
    {
        private Timer _timer;
        private readonly MainWindow window;
        private bool pause;
        public Counter(MainWindow mainWindow)
        {
            window = mainWindow;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new Timer
            {
                Interval = 1000
            };
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (pause)
            {
                return;
            }
            Dispatcher.Invoke(() =>
            {
                LiteDatabase _liteDatabase = new LiteDatabase(window.liteDBString);
                ILiteCollection<ChatData> chat = _liteDatabase.GetCollection<ChatData>("chat");
                List<string> filters = filter.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                List<ChatData> list = chat.FindAll().ToList();
                if (filters.Count > 0)
                {
                    foreach (string filter in filters.ToList())
                    {
                        if (filter.Contains("-"))
                        {
                            string[] predict = filter.Split('-');
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
                                _ = filters.Remove(filter);
                                filters.AddRange(Enumerable.Range(min, max - min + 1).Select(x => x.ToString()));
                            }
                        }
                    }
                    IEnumerable<ChatData> result = list.Where((x) =>
                    {
                        foreach (string filter in filters.ToList())
                        {
                            if (int.TryParse(filter, out int compare))
                            {
                                if (int.TryParse(x.Comment, out int comment))
                                {
                                    if (compare == comment)
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
                    foreach (string filter in filters)
                    {
                        if (int.TryParse(filter, out int num))
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
                    var result = list.AsEnumerable();
                    if (showOnce.IsChecked ?? false)
                    {
                        result = result.GroupBy(x => x.User).Select(y => y.First());
                    }
                    grid.ItemsSource = result;
                    Count.Content = result.Count();
                    List<CounterData> counters = new List<CounterData>();
                    counter.ItemsSource = counters;
                }
                _liteDatabase.Dispose();
            });

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
    }
}
