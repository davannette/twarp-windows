using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace Twarp
{
    class TwitterInterface
    {
        // oAuth settings - TODO: obfuscate!
        private const string TWITTER_KEY = "WToBvaDrAupPFFTHA5t5C02u4";
        private const string TWITTER_SECRET = "LzKTYKL4PJbiUzijDJO6gc9UBI1LeeTOiIiN3as3LNd8eJlFC0";

        public TwitterSearchResults data;
        private TwitterTimer timer;

        public string hashtag;

        private string accessToken = "";

        public long startID = 0;
        private DateTime startTime;
        private long tweetRate = 0;

        public TwitterInterface(string tag, DateTime date)
        {
            hashtag = tag;
            startTime = date;
        }

        public async Task init()
        {
            // need access token first:
            await getAccessToken();

            // find start tweet ID:
            await findStartID(startTime);
        }

        private async Task getAccessToken()
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth2/token ");
            var customerInfo = Convert.ToBase64String(new UTF8Encoding()
                                      .GetBytes(TWITTER_KEY + ":" + TWITTER_SECRET));
            request.Headers.Add("Authorization", "Basic " + customerInfo);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8,
                                                                      "application/x-www-form-urlencoded");

            HttpResponseMessage response = await httpClient.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();
            accessToken = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(json).access_token;
        }

        public async Task findStartID(DateTime date)
        {
            // var lowerBound = date.ToUniversalTime().Date;
            // var upperBound = lowerBound.AddDays(1);

            var upperBound = DateTime.UtcNow.Date;
            var lowerBound = upperBound.AddDays(-1);
            var dayStart = date.ToUniversalTime().Date;

            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            // lower bound:
            var request = new HttpRequestMessage(HttpMethod.Get, String.Format(
                "https://api.twitter.com/1.1/search/tweets.json?q=a&count=1&until={0:yyyy-MM-dd}&include_entities=false", lowerBound));
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<TwitterSearchResults>(json);
            var lowerBoundID = results.statuses[0].id;
            DateTime lowerDate = DateTime.ParseExact(results.statuses[0].created_at, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);

            // upper bound:
            request = new HttpRequestMessage(HttpMethod.Get, String.Format(
                "https://api.twitter.com/1.1/search/tweets.json?q=a&count=1&until={0:yyyy-MM-dd}&include_entities=false", upperBound));
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            response = await httpClient.SendAsync(request);
            json = await response.Content.ReadAsStringAsync();
            results = JsonConvert.DeserializeObject<TwitterSearchResults>(json);
            var upperBoundID = results.statuses[0].id;
            DateTime upperDate = DateTime.ParseExact(results.statuses[0].created_at, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);

            // calculate Tweet rate:
            long seconds = (upperDate.Ticks - lowerDate.Ticks) / 10000000;
            tweetRate = (upperBoundID - lowerBoundID) / seconds;

            // start of day:
            request = new HttpRequestMessage(HttpMethod.Get, String.Format(
                "https://api.twitter.com/1.1/search/tweets.json?q=a&count=1&until={0:yyyy-MM-dd}&include_entities=false", dayStart));
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            response = await httpClient.SendAsync(request);
            json = await response.Content.ReadAsStringAsync();
            results = JsonConvert.DeserializeObject<TwitterSearchResults>(json);
            var dayStartID = results.statuses[0].id;

            long offset = Convert.ToInt64(date.ToUniversalTime().TimeOfDay.TotalSeconds);

            startID = dayStartID + (offset * tweetRate);

            // start timer:
            timer = new TwitterTimer(date);
        }

        public async Task SearchTimeline(long elapsed = 0, uint count = 100)
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            var searchID = startID + elapsed * tweetRate;

            var request = new HttpRequestMessage(HttpMethod.Get, String.Format(
                "https://api.twitter.com/1.1/search/tweets.json?q=%23{0}&max_id={1}&count={2}", hashtag, searchID, count));
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            data = JsonConvert.DeserializeObject<TwitterSearchResults>(json);
        }

        public async Task next(int count)
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            //var expr = new Regex("count=\\d+");
            //var searchStr = expr.Replace(data.search_metadata.next_results, String.Format("count={0}", count));
            var request = new HttpRequestMessage(HttpMethod.Get,
                String.Format("https://api.twitter.com/1.1/search/tweets.json{0}", data.search_metadata.next_results));
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            data = JsonConvert.DeserializeObject<TwitterSearchResults>(json);
        }

        private class TwitAuthenticateResponse
        {
            public string token_type { get; set; }
            public string access_token { get; set; }
        }

        public static bool IsInternet()
        {
            return true;
            //ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            //bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            //return internet;
        }
    }
}
