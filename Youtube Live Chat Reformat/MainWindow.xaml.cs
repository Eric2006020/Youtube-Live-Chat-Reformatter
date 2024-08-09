using CefSharp;
using CefSharp.Wpf;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;


namespace Youtube_Live_Chat_Reformat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private YoutubeService _youtubeService;
        private string Url;
        public string liteDBString;
        private Counter Counter;
        public MainWindow()
        {
            CefSettings settings = new CefSettings
            {
                CachePath = Path.GetFullPath("cache"),
            };
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "https",
                DomainName = "live.youtube.chat",
                SchemeHandlerFactory = new ResourceHandlerService()
            });
            if (!Directory.Exists("Temp"))
            {
                _ = Directory.CreateDirectory("Temp");
            }
            _ = Cef.Initialize(settings);
            InitializeComponent();
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            Border sp = sender as Border;
            DoubleAnimation db = new DoubleAnimation();
            //db.From = 12;
            db.To = 30;
            db.Duration = TimeSpan.FromSeconds(0.5);
            db.AutoReverse = false;
            db.RepeatBehavior = new RepeatBehavior(1);
            sp.BeginAnimation(StackPanel.HeightProperty, db);
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            Border sp = sender as Border;
            DoubleAnimation db = new DoubleAnimation();
            //db.From = 12;
            db.To = 1;
            db.Duration = TimeSpan.FromSeconds(0.5);
            db.AutoReverse = false;
            db.RepeatBehavior = new RepeatBehavior(1);
            sp.BeginAnimation(StackPanel.HeightProperty, db);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _youtubeService?.Dispose();
            Url = UrlTextBox.Text;
            FilterPanel.Visibility = Visibility.Visible;
            try
            {
                Uri uri = new Uri(Url);
                NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
                _youtubeService = new YoutubeService();
                liteDBString = "Filename=Temp\\" + query.Get("v") + ";Connection=shared; journal=false";
                _youtubeService.CommentReceived += _youtubeService_CommentReceived;
                _youtubeService.InitChromium(Url, browser);
                if (Counter != null)
                {
                    Counter.Reset();
                }
            }
            catch
            {
                _ = MessageBox.Show("Invalid Url!", "Operation failed successfully!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void _youtubeService_CommentReceived(object sender, YoutubeService.CommentEvent e)
        {
            LiteDatabase _liteDatabase = new LiteDatabase(liteDBString);
            ILiteCollection<ChatData> collection = _liteDatabase.GetCollection<ChatData>("chat");
            IEnumerable<ChatData> list = collection.FindAll();
            ChatData last = list.Count() > 0 ? list.Last() : new ChatData();
            if (!(last.User == e.User && last.Comment == e.Comment))
            {
                var insert = new ChatData
                {
                    Comment = e.Comment,
                    User = e.User,
                    SCAmount = e.SuperChat ? e.SuperChatAmount : 0
                };
                _ = collection.Insert(insert);
                if (Counter != null)
                {
                    Counter.AddMessage(insert);
                }
            }
            _liteDatabase.Dispose();
        }

        private void FilterWindow(object sender, RoutedEventArgs e)
        {
            if(Counter != null)
            {
                return;
            }
            Counter counter = new Counter(this);
            Counter = counter;
            counter.Closing += Counter_Closing;
            counter.Show();
        }

        private void Counter_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Counter = null;
        }
    }
}
