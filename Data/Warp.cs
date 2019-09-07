using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twarp.Data
{
    class Warp
    {
        public string hashtag { get; set; }
        public DateTime startTime { get; set; }
        public String day
        {
            get
            {
                return startTime.ToString("dddd");
            }
        }
        public String time
        {
            get
            {
                return startTime.ToString("HH:mm");
            }
        }

        public Warp(string tag, DateTime date)
        {
            hashtag = tag;
            startTime = date;
        }
    }
}
