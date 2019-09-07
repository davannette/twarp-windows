using System;
using System.Linq;
using Twarp.Data;
using Twarp.UserControls;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Twarp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Pivot twarpPivot = new Pivot()
        {
            Title = "",
            Margin = new Thickness { Top = -25 }
        };

        private FavouritesHistory favHist = new FavouritesHistory();
        private Settings twarpSettings = new Settings();

        private enum app_stage { main, fav, settings, feeder, timeline, tweet };
        private app_stage stage = app_stage.main;
        public Boolean Mobile = false;
        public Boolean Desktop = false;

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Resuming += Current_Resuming;

            // check if we're on a mobile device:
            Mobile = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile";
            Desktop = !Mobile;

            // create menu for desktop version
            if (Desktop)
            {
                // desktop version of the app - create the menu:
                menuContent.Children.Insert(0, favHist);
                // menuContent.Children.Insert(1, twarpSettings);
            }

            // initialise favourites and wire up events:
            favHist.init();
            favHist.FavouriteEvent += Favourites_FavouriteEvent;
            favHist.HistoryEvent += Favourites_HistoryEvent;

            // add event handler for pivot item change:
            twarpPivot.SelectionChanged += TwarpPivot_SelectionChanged;

            // load feed selection user control and wire up handler:
            // mainContent.Content = feed;
            FeedSelector feed = new FeedSelector();
            feed.TwarpEvent += Feed_TwarpEvent;
            feed.AddFavEvent += Feed_AddFavEvent;

            // add feed to first item of pivot:
            if (Mobile)
            {
                mainContent.Content = twarpPivot;

                // twarpPivot.Margin = new Thickness { Top = -25 };
                PivotItem pi = new PivotItem() { Header = "New feed" };
                pi.Content = feed;
                twarpPivot.Items.Add(pi);
                twarpPivot.SelectedIndex = 0;
            } else
            {
                mainStack.Children.Add(feed);
                Grid.SetColumn(feed, 0);
            }


            SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) =>
            {
                // mobile only...

                // Restore to previous stage:
                switch (stage)
                {
                    case app_stage.tweet:
                        PivotItem pItem = (PivotItem)twarpPivot.Items[twarpPivot.SelectedIndex];
                        ViewTweet tweet = (ViewTweet)pItem.Content;
                        pItem.Content = tweet.timeline;
                        stage = app_stage.timeline;
                        // SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                        e.Handled = true;
                        break;
                    case app_stage.timeline:
                        // return to feed selector:
                        PivotItem pItem2 = (PivotItem)twarpPivot.Items[twarpPivot.SelectedIndex];
                        pItem2.Header = "New feed";
                        pItem2.Content = ((TwarpTimeline)pItem2.Content).feeder;
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                        stage = app_stage.feeder;
                        e.Handled = true;
                        break;
                    case app_stage.settings:
                    case app_stage.fav:
                        mainContent.Content = twarpPivot;
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                        stage = app_stage.main;
                        e.Handled = true;
                        break;
                    default:
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                        e.Handled = false;
                        break;
                }

            };

        }

        private void Current_Resuming(object sender, object e)
        {
            // App resuming, check age of any active timelines, refresh those that are still current:
            DateTime old = DateTime.Now.AddMinutes(-30);
            if (Mobile)
            {
                foreach (PivotItem item in twarpPivot.Items)
                {
                    if (item.Content is TwarpTimeline)
                    {
                        TwarpTimeline timeline = item.Content as TwarpTimeline;
                        if (timeline.lastRefresh > old)
                        {
                            // timeline is less than 30 minutes old, refresh it:
                            timeline.refresh();
                            return;
                        } else
                        {
                            // timeline is old, return to feed selector:
                            item.Content = timeline.feeder;
                        }
                    }
                }
            } else
            {
                foreach (var item in mainStack.Children)
                {
                    if (item is TwarpTimeline)
                    {
                        TwarpTimeline timeline = item as TwarpTimeline;
                        if (timeline.lastRefresh > old)
                        {
                            // timeline is less than 30 minutes old, let's refresh it:
                            timeline.refresh();
                            return;
                        }
                        else
                        {
                            // item is old, return to feed selector:
                            var index = mainStack.Children.IndexOf(item);
                            mainStack.Children[index] = item;
                        }
                    }
                }
            }
        }

        private void TwarpPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // mobile only...
            PivotItem  pi = (PivotItem)(sender as Pivot).SelectedItem;
            if (pi.Content is TwarpTimeline)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                stage = app_stage.timeline;
            } else if (pi.Content is ViewTweet)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                stage = app_stage.tweet;
            } else
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                stage = app_stage.feeder;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void favourites_Click(object sender, RoutedEventArgs e)
        {
            // Mobile only...

            if (twarpPivot.Items.Count >= 4)
            {
                Boolean available = false;
                foreach (PivotItem item in twarpPivot.Items)
                {
                    if ((String)item.Header == "New feed")
                    {
                        available = true;
                        break;
                    }
                }
                if (!available)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }
            }

            mainContent.Content = favHist;

            // enable back button
            stage = app_stage.fav;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private void feeder_Click(object sender, RoutedEventArgs e)
        {
            // close menu (if on desktop):
            if (Desktop)
            {
                MainSplitView.IsPaneOpen = false;
            }

            // disable back button - mobile only
            if (Mobile)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

                // see if we already have a new feed tab, then select it:
                for (var i = 0; i < twarpPivot.Items.Count; i++)
                {
                    if ((String)((PivotItem)twarpPivot.Items[i]).Header == "New feed")
                    {
                        twarpPivot.SelectedIndex = i;
                        mainContent.Content = twarpPivot;
                        return;
                    }
                }

                if (twarpPivot.Items.Count >= 4)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }
            }
            else {
                // see if we already have a feed selector open, if so don't create a new one:
                foreach (var item in mainStack.Children)
                {
                    if (item is FeedSelector)
                    {
                        MessageDialog dialog = new MessageDialog("You already have a feed selector open.", "Already open");
                        dialog.ShowAsync();
                        return;
                    }
                }

                if (mainStack.Children.Count >= 4)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }

            }

            // create new feed selector:
            FeedSelector feed = new FeedSelector();
            feed.TwarpEvent += Feed_TwarpEvent;
            feed.AddFavEvent += Feed_AddFavEvent;

            if (Mobile)
            {

                // add feed to first item of pivot:
                // if (twarpPivot.Items.Count > 0)
                //    twarpPivot.Margin = new Thickness { Top = -50 };
                PivotItem pi = new PivotItem() { Header = "New feed" };
                pi.Content = feed;
                twarpPivot.Items.Add(pi);
                twarpPivot.SelectedIndex = twarpPivot.Items.Count - 1;
            } else
            {
                var col = new ColumnDefinition();
                col.Width = new GridLength(1, GridUnitType.Star);
                mainStack.ColumnDefinitions.Add(col); mainStack.Children.Add(feed);
                Grid.SetColumn(feed, mainStack.Children.Count - 1);
            }
        }

        private void Feed_AddFavEvent(object sender, EventArgs e)
        {
            FeedSelector feeder = sender as FeedSelector;
            favHist.addFavourite(feeder.hashtag, feeder.startTime);

            MessageDialog dialog = new MessageDialog("Search has been added to your favourites.", "Favourite added");
            dialog.ShowAsync();
        }

        private async void Feed_TwarpEvent(object sender, EventArgs e)
        {

            FeedSelector feeder = sender as FeedSelector;
            if (feeder.hashtag.Length <= 2)
            {
                MessageDialog dialog = new MessageDialog("The hashtag must be greater than two characters in length.", "Invalid hashtag");
                dialog.ShowAsync();
                return;
            }

            TwarpTimeline timeline = new TwarpTimeline(Mobile);
            timeline.feeder = feeder;
            timeline.TweetEvent += Timeline_TweetEvent;
            timeline.CloseEvent += Timeline_CloseEvent;
            timeline.BackEvent += Timeline_BackEvent;

            await timeline.init(feeder.hashtag, feeder.startTime);
            if (timeline.noNetwork)
            {
                MessageDialog dialog = new MessageDialog("Please connect to the internet and try again.", "No internet connection");
                dialog.ShowAsync();
                return;
            }
            if (timeline.noData)
            {
                MessageDialog dialog = new MessageDialog("The twitter search returned no results. Try using a different hashtag.", "No results found");
                dialog.ShowAsync();
                return;
            }

            if (Mobile)
            {
                PivotItem pi = feeder.Parent as PivotItem;
                pi.Content = timeline;
                pi.Header = "#" + feeder.hashtag;
            } else
            {
                var index = mainStack.Children.IndexOf(feeder);
                mainStack.Children[index] = timeline;
                Grid.SetColumn(timeline, index);
            }

            // add history item:
            favHist.addHistory(feeder.hashtag, feeder.startTime);

            // enable back button
            if (Mobile)
            {
                stage = app_stage.timeline;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
        }

        private void Timeline_BackEvent(object sender, EventArgs e)
        {
          // this is only called in the desktop version:
            var index = mainStack.Children.IndexOf((TwarpTimeline)sender);
            mainStack.Children[index] = ((TwarpTimeline)sender).feeder;
        }

        private void Timeline_NoDataEvent(object sender, EventArgs e)
        {
            MessageDialog dialog = new MessageDialog("The twitter search returned no tweets. Try a different hashtag.", "No results");
            dialog.ShowAsync();
        }

        private void Timeline_CloseEvent(object sender, EventArgs e)
        {
            if (Mobile)
            {
                PivotItem pi = (sender as TwarpTimeline).Parent as PivotItem;
                if (twarpPivot.Items.Count > 1)
                {
                    twarpPivot.Items.Remove(pi);
                }
                else
                {
                    // restore and reset the feed selector:
                    FeedSelector feed = (sender as TwarpTimeline).feeder;
                    feed.hashtag = "";
                    feed.startTime = DateTime.Now;

                    pi.Header = "New feed";
                    pi.Content = feed;

                }
            } else
            {
                if (mainStack.ColumnDefinitions.Count > 1)
                {
                    mainStack.Children.Remove(sender as TwarpTimeline);
                    renumberColumns(mainStack);
                    mainStack.ColumnDefinitions.RemoveAt(0);
                } else
                {
                    var index = mainStack.Children.IndexOf(sender as TwarpTimeline);
                    FeedSelector feed = (sender as TwarpTimeline).feeder;
                    feed.hashtag = "";
                    feed.startTime = DateTime.Now;
                    mainStack.Children[index] = feed;
                }
            }
        }

        private void Timeline_TweetEvent(object sender, EventArgs e)
        {
            ViewTweet tweet = new ViewTweet((Status)sender, Mobile);
            tweet.BackEvent += Tweet_BackEvent;

            if (Mobile)
            {
                PivotItem pi = (PivotItem)twarpPivot.Items[twarpPivot.SelectedIndex];
                tweet.timeline = (TwarpTimeline)pi.Content;
                pi.Content = tweet;
            } else
            {
                foreach(var item in mainStack.Children)
                {
                    if (item is TwarpTimeline && ((TwarpTimeline)item).active)
                    {
                        var index = mainStack.Children.IndexOf(item);
                        tweet.timeline = (TwarpTimeline)mainStack.Children[index];
                        Grid.SetColumn(tweet, index);
                        mainStack.Children[index] = tweet;
                        break;
                    }
                }
            }
            // enable back button - mobile only
            if (Mobile)
            {
                stage = app_stage.tweet;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
        }

        private void Tweet_BackEvent(object sender, EventArgs e)
        {
            // this is only called in the desktop version:
            var index = mainStack.Children.IndexOf((ViewTweet)sender);
            mainStack.Children[index] = ((ViewTweet)sender).timeline;
        }

        private void Favourites_FavouriteEvent(object sender, EventArgs e)
        {
            // close menu (if on desktop):
            if (Desktop)
            {
                MainSplitView.IsPaneOpen = false;
            }

            // Get favourite from parameter:
            Favourite fav = sender as Favourite;

            if (Mobile)
            {
                // disable back button - mobile only
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

                mainContent.Content = twarpPivot;

                // see if we already have a new feed tab, then select it:
                for (var i = 0; i < twarpPivot.Items.Count; i++)
                {
                    PivotItem item = (PivotItem)twarpPivot.Items[i];
                    if ((String)item.Header == "New feed")
                    {
                        FeedSelector fs = item.Content as FeedSelector;
                        fs.hashtag = fav.hashtag;
                        DateTime dt = DateTime.Now.Date;
                        while (fav.day != dt.ToString("dddd"))
                            dt = dt.AddDays(-1);
                        fs.startTime = dt.AddMinutes(fav.minutes);

                        twarpPivot.SelectedIndex = i;
                        return;
                    }
                }

                if (twarpPivot.Items.Count >= 4)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }
            } else
            {
                // see if we already have a feed selector open, then replace it:
                foreach (var item in mainStack.Children)
                {
                    if (item is FeedSelector) {
                        ((FeedSelector)item).hashtag = fav.hashtag;
                        DateTime dt = DateTime.Now.Date;
                        while (fav.day != dt.ToString("dddd"))
                            dt = dt.AddDays(-1);
                        ((FeedSelector)item).startTime = dt.AddMinutes(fav.minutes);
                        return;
                    }
                }

                if (mainStack.Children.Count >= 4)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }
            }

            FeedSelector feeder = new FeedSelector();
            feeder.TwarpEvent += Feed_TwarpEvent;
            feeder.AddFavEvent += Feed_AddFavEvent;
            feeder.hashtag = fav.hashtag;
            DateTime date = DateTime.Now.Date;
            while (fav.day != date.ToString("dddd"))
                date = date.AddDays(-1);
            feeder.startTime = date.AddMinutes(fav.minutes);

            if (Mobile)
            {
                PivotItem pi = new PivotItem();
                pi.Content = feeder;
                pi.Header = "New feed";

                twarpPivot.Items.Add(pi);
                twarpPivot.SelectedIndex = twarpPivot.Items.Count - 1;
            } else
            {
                var col = new ColumnDefinition();
                col.Width = new GridLength(1, GridUnitType.Star);
                mainStack.ColumnDefinitions.Add(col);
                mainStack.Children.Add(feeder);
                Grid.SetColumn(feeder, mainStack.Children.Count() - 1);
            }
        }

        private void Favourites_HistoryEvent(object sender, EventArgs e)
        {
            // close menu (if on desktop):
            if (Desktop)
            {
                MainSplitView.IsPaneOpen = false;
            }

            // Get favourite from parameter:
            Warp hist = sender as Warp;

            if (Mobile)
            {
                // disable back button - mobile only
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

                mainContent.Content = twarpPivot;

                // see if we already have a new feed tab, then select it:
                for (var i = 0; i < twarpPivot.Items.Count; i++)
                {
                    PivotItem item = (PivotItem)twarpPivot.Items[i];
                    if ((String)item.Header == "New feed")
                    {
                        FeedSelector fs = item.Content as FeedSelector;
                        fs.hashtag = hist.hashtag;
                        fs.startTime = hist.startTime;

                        twarpPivot.SelectedIndex = i;
                        return;
                    }
                }

                if (twarpPivot.Items.Count >= 4)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }
            }
            else
            {
                // see if we already have a feed selector open, then replace it:
                foreach (var item in mainStack.Children)
                {
                    if (item is FeedSelector)
                    {
                        ((FeedSelector)item).hashtag = hist.hashtag;
                        ((FeedSelector)item).startTime = hist.startTime;
                        return;
                    }
                }

                if (mainStack.Children.Count >= 4)
                {
                    MessageDialog dialog = new MessageDialog("The number of timelines to follow is limited to four.", "Cannot add new search");
                    dialog.ShowAsync();
                    return;
                }
            }

            FeedSelector feeder = new FeedSelector();
            feeder.TwarpEvent += Feed_TwarpEvent;
            feeder.AddFavEvent += Feed_AddFavEvent;
            feeder.hashtag = hist.hashtag;
            feeder.startTime = hist.startTime;

            if (Mobile)
            {
                PivotItem pi = new PivotItem();
                pi.Content = feeder;
                pi.Header = "New feed";

                twarpPivot.Items.Add(pi);
                twarpPivot.SelectedIndex = twarpPivot.Items.Count - 1;
            }
            else
            {
                var col = new ColumnDefinition();
                col.Width = new GridLength(1, GridUnitType.Star);
                mainStack.ColumnDefinitions.Add(col);
                mainStack.Children.Add(feeder);
                Grid.SetColumn(feeder, mainStack.Children.Count() - 1);
            }
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            // mobile only...

            mainContent.Content = twarpSettings;

            // enable back button
            stage = app_stage.settings;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
        }

        private void renumberColumns(Grid grid)
        {
            for (var i=0; i<grid.Children.Count; i++)
            {
                if (grid.Children[i] is ViewTweet)
                {
                    var tweet = grid.Children[i] as ViewTweet;
                    Grid.SetColumn(tweet, i);
                    Grid.SetColumn(tweet.timeline, i);
                    Grid.SetColumn(tweet.timeline.feeder, i);
                } else if (grid.Children[i] is TwarpTimeline)
                {
                    var timeline = grid.Children[i] as TwarpTimeline;
                    Grid.SetColumn(timeline, i);
                    Grid.SetColumn(timeline.feeder, i);
                } else
                {
                    Grid.SetColumn(grid.Children[i] as FeedSelector, i);
                }
            }
        }
    }
}
