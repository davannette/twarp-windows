using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twarp.Data
{
    class Favourite
    {
        public String day { get; set; }
        public String time { get; set; }
        public int minutes { get; set; }
        public String hashtag { get; set; }

        public Favourite(String tag, DateTime date)
        {
            hashtag = tag;
            day = date.ToString("dddd");
            time = date.ToString("HH:mm");
            minutes = date.Hour * 60 + date.Minute;
        }
    }
}
