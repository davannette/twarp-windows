using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Twarp.Data;
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

namespace Twarp.UserControls
{
    public sealed partial class FeedSelector : UserControl
    {
        public event EventHandler TwarpEvent;
        public event EventHandler AddFavEvent;

        public String hashtag
        {
            get { return txHashtag.Text; }
            set { txHashtag.Text = value; }
        }

        // view display details:
        public Object viewItem { get; set; }
        public Boolean pivot { get; set; } = false;
        public Boolean stack { get; set; } = false;

        public DateTime startTime
        {
            get
            {
                return DateTime.Now.Date.AddDays(-1 * day.SelectedIndex).AddMinutes(timeSlider.Value);
            }
            set
            {
                var searchday = value.ToString("dddd");
                var index = days.IndexOf(searchday);
                if (index == -1) index = 0;
                day.SelectedIndex = index;
                timeSlider.Value = value.Hour * 60 + value.Minute;
                time.Text = String.Format("{0}:{1}", value.Hour.ToString("D2"), value.Minute.ToString("D2"));
            }
        }

        private List<String> days = new List<String>();

        public FeedSelector()
        {
            this.InitializeComponent();

            // set ComboBox days:
            var date = DateTime.Now.Date;
            days.Add("Today");
            for (int i = 1; i < 8; i++)
            {
                days.Add(date.AddDays(-1 * i).ToString("dddd"));
            }
            day.ItemsSource = days;

            // set search time to now:
            day.SelectedIndex = 0;
            timeSlider.Value = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            time.Text = String.Format("{0}:{1}", DateTime.Now.Hour.ToString("D2"), DateTime.Now.Minute.ToString("D2"));
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (day.SelectedIndex == 0 && slider.Value > (DateTime.Now.Hour * 60 + DateTime.Now.Minute))
                slider.Value = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            int hours = Convert.ToInt32(slider.Value) / 60;
            int minutes = Convert.ToInt32(slider.Value) % 60;
            time.Text = String.Format("{0}:{1}", hours.ToString("D2"), minutes.ToString("D2"));
        }

        private void twarp_Click(object sender, RoutedEventArgs e)
        {
            if (this.TwarpEvent != null)
            {
                this.TwarpEvent(this, new EventArgs());
            }
        }

        private void addfav_Click(object sender, RoutedEventArgs e)
        {
            if (this.AddFavEvent != null)
            {
                this.AddFavEvent(this, new EventArgs());
            }
            // DateTime datetime = DateTime.Now.Date.AddDays(-1 * day.SelectedIndex).AddMinutes(timeSlider.Value);

            //Warp search = new Warp(hashtag.Text, datetime);
            //Frame.Navigate(typeof(FavouritesHistory), search);
        }
    }
}
