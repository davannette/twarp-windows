using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Twarp.Data;
using Twarp.UserControls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Twarp
{
    public sealed partial class TwarpTimeline : UserControl
    {
        public event EventHandler TweetEvent;
        public event EventHandler CloseEvent;
        public event EventHandler BackEvent;
        public event EventHandler NoDataEvent;

        private TwitterDataSource twitter;
        private TwitterTimer timer;

        public Boolean active = false;

        // feed properties:
        public String hashtag { get; set; }
        public DateTime startTime { get; set; }
        public FeedSelector feeder { get; set; }
        public Boolean noData { get; set; } = true;
        public Boolean noNetwork { get; set; } = true;

        public DateTime lastRefresh { get; set; }

        private DispatcherTimer dispatcherTimer;

        public TwarpTimeline(Boolean isMobile)
        {
            this.InitializeComponent();

            if (isMobile)
            {
                Back.Visibility = Visibility.Collapsed;
            }
        }

        public async void refresh()
        {
            await twitter.refresh(timer.elapsed());
        }

        public async Task init(String tag, DateTime start)
        {
            hashtag = tag;
            startTime = start;

            twitter = new TwitterDataSource(tag, start);
            await twitter.init();

            // make sure we have a network connection and some data:
            if ((noNetwork = twitter.noNetwork) || (noData = twitter.noData))
            {
                return;
            }

            // start timer:
            timer = new TwitterTimer(start);

            // start clock display:
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            listView.ItemsSource = twitter;

            lastRefresh = DateTime.Now;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await twitter.refresh(timer.elapsed());
            // listView.ItemsSource = twitter.data.statuses;
            lastRefresh = DateTime.Now;
        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            RTClock_date.Text = timer.getDate();
            RTClock_time.Text = timer.getTime();
        }

        private async void JumpBack_Click(object sender, RoutedEventArgs e)
        {
            timer.skip(TwitterTimer.Skip.BIG, TwitterTimer.Direction.BACK);

            await twitter.refresh(timer.elapsed());
            // listView.ItemsSource = twitter.data.statuses;
        }

        private async void JumpForward_Click(object sender, RoutedEventArgs e)
        {
            timer.skip(TwitterTimer.Skip.BIG, TwitterTimer.Direction.FORWARD);

            await twitter.refresh(timer.elapsed());
            // listView.ItemsSource = twitter.data.statuses;
        }

        private async void SkipBack_Click(object sender, RoutedEventArgs e)
        {
            timer.skip(TwitterTimer.Skip.SMALL, TwitterTimer.Direction.BACK);

            await twitter.refresh(timer.elapsed());
            // listView.ItemsSource = twitter.data.statuses;
        }

        private async void SkipForward_Click(object sender, RoutedEventArgs e)
        {
            timer.skip(TwitterTimer.Skip.SMALL, TwitterTimer.Direction.FORWARD);

            await twitter.refresh(timer.elapsed());
            // listView.ItemsSource = twitter.data.statuses;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (timer.togglePause())
                Pause.Content = "PLAY";
            else
                Pause.Content = "PAUSE";
        }

        private void tweet_clicked(object sender, ItemClickEventArgs e)
        {
            active = true;
            Status item = (Status)e.ClickedItem;
            if (this.TweetEvent != null)
            {
                this.TweetEvent(item, new EventArgs());
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            // PivotItem item = this.Parent as PivotItem;

            if (this.CloseEvent != null)
            {
                this.CloseEvent(this, new EventArgs());
            }

        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (this.BackEvent != null)
            {
                this.BackEvent(this, new EventArgs());
            }

        }

        private async void ptRefresh_RefreshInvoked(DependencyObject sender, object args)
        {
            await twitter.refresh(timer.elapsed());
            lastRefresh = DateTime.Now;
        }
    }
}
