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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _youtubeService?.Dispose();
            Url = UrlTextBox.Text;
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
