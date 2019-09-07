using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Twarp.Data
{
    class TwitterDataSource : ObservableCollection<Status>, ISupportIncrementalLoading
    {
        private TwitterInterface twitter = null;
        private String hashtag;
        private DateTime startTime;

        private Boolean hasMoreItems = true;

        public Boolean noData { get; set; } = true;
        public Boolean noNetwork { get; set; }

        public TwitterDataSource(String tag, DateTime date)
        {
            hashtag = tag;
            startTime = date;
        }

        public async Task init()
        {
            twitter = new TwitterInterface(hashtag, startTime);
            noNetwork = !TwitterInterface.IsInternet();

            if (noNetwork)
                return;

            await twitter.init();
            await twitter.SearchTimeline(0, 40);
            if (twitter.data.statuses.Count != 0)
            {
                AddRange(twitter.data.statuses);
                noData = false;
            }
        }

        public bool HasMoreItems
        {
            get
            {
                return hasMoreItems;
            }
        }

        public async Task refresh(long elapsed)
        {
            await twitter.SearchTimeline(elapsed, 40);
            ClearItems();
            AddRange(twitter.data.statuses);
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var dispatcher = Window.Current.Dispatcher;

            return Task.Run<LoadMoreItemsResult>(
                async () =>
                {
                    await twitter.next((int)count);

                    if (twitter.data.search_metadata.count > 0)
                    {
                        hasMoreItems = true;
                        await dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            foreach (Status item in twitter.data.statuses)
                                this.Add(item);
                        });
                    }


                    return new LoadMoreItemsResult() { Count = Convert.ToUInt16(twitter.data.statuses.Count()) };

                }).AsAsyncOperation<LoadMoreItemsResult>();

        }

        public void AddRange(IEnumerable<Status> list)
        {
            if (list == null) return;
            foreach(var item in list) {
                Items.Add(item);
            }
        }

    }
}
