using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Twarp.Data;
using Twarp.Storage;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Twarp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FavouritesHistory : UserControl
    {
        // event handlers:
        public event EventHandler FavouriteEvent;
        public event EventHandler HistoryEvent;

        public Boolean loaded { get; set; } = false;

        private LocalFile<ObservableCollection<Favourite>> favouritesFile = new LocalFile<ObservableCollection<Favourite>>("favourites.txt");
        private LocalFile<ObservableCollection<Warp>> historyFile = new LocalFile<ObservableCollection<Warp>>("history.txt");

        public FavouritesHistory()
        {
            this.InitializeComponent();
            // favouritesFile.load();
            // historyFile.load();
        }

        public async Task init()
        {

            // load favourites:
            await favouritesFile.load();
            if (favouritesFile.data == null)
            {
                favouritesFile.data = new ObservableCollection<Favourite>();
            }
            FavouritesListView.ItemsSource = favouritesFile.data;

            // load history:
            await historyFile.load();
            if (historyFile.data == null)
            {
                historyFile.data = new ObservableCollection<Warp>();
            }
            HistoryListView.ItemsSource = historyFile.data;

            loaded = true;
        }

        public Boolean addFavourite(String tag, DateTime date) {
            foreach(var item in favouritesFile.data)
            {
                if (item.hashtag == tag)
                {
                    return false;
                }
            }
            favouritesFile.data.Add(new Favourite(tag, date));
            favouritesFile.save();
            return true;
        }

        public void addHistory(String tag, DateTime date)
        {

            historyFile.data.Insert(0, new Warp(tag, date));

            if (historyFile.data.Count == 6)
            {
                // truncate list:
                historyFile.data.RemoveAt(6);
            }
            historyFile.save();
        }

        private async void FavouritesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Favourite fav = (Favourite)e.ClickedItem;
            if (this.FavouriteEvent != null)
            {
                this.FavouriteEvent(fav, new EventArgs());
            }
            await Task.Delay(100);
            ((ListView)sender).SelectedItem = null;
        }

        private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Warp history = (Warp)e.ClickedItem;
            if (this.HistoryEvent != null)
            {
                this.HistoryEvent(history, new EventArgs());
            }
            await Task.Delay(100);
            ((ListView)sender).SelectedItem = null;
        }
    }
}
