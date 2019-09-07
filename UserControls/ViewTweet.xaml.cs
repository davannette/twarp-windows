using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Twarp.UserControls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ViewTweet : UserControl
    {
        public event EventHandler BackEvent;

        public TwarpTimeline timeline;

        public ViewTweet(Status tweet, Boolean isMobile)
        {
            this.InitializeComponent();

            if (isMobile)
            {
                Back.Visibility = Visibility.Collapsed;
            }

            DataContext = tweet;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (this.BackEvent != null)
            {
                this.BackEvent(this, new EventArgs());
            }

        }

    }

}
