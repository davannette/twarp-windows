using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twarp
{
    class TwitterTimer
    {

        // direction/scale enums:
        public enum Skip { SMALL, BIG };
        public enum Direction { BACK, FORWARD };

        // constant for converting to TICKS:
        private const long TICKS = 10000000;

        // time skip constants:
        public const long SMALL_SKIP = 30 * TICKS; // 30 seconds
        public const long BIG_SKIP = 5 * 60 * TICKS; // 5 minutes

        // timer properties:
        private DateTime created;
        private DateTime origin;
        private long offset;

        // pause functionality
        public Boolean paused = false;
        private DateTime timePaused;

        // constructor
        public TwitterTimer(DateTime date)
        {
            created = DateTime.Now;
            origin = date;
        }

        public String getTime()
        {
            var timePassed = elapsedTime(created) + offset;
            if (paused)
                timePassed -= elapsedTime(timePaused);
            return origin.AddTicks(timePassed).ToString("HH:mm:ss");
        }

        public String getDate()
        {
            var timePassed = elapsedTime(created) + offset;
            if (paused)
                timePassed -= elapsedTime(timePaused);
            DateTime date = origin.AddTicks(timePassed);
            return date.ToString("ddd d") + suffix(date);
        }

        public long elapsed()
        {
            var timePassed = elapsedTime(created) + offset;
            if (paused)
                timePassed -= elapsedTime(timePaused);
            return timePassed / TICKS;
        }

        public void skip(Skip skip, Direction dir)
        {
            long amt =  (skip == Skip.SMALL ? SMALL_SKIP : BIG_SKIP);
            offset += (dir == Direction.BACK ? -1 * amt : amt);
        }

        public Boolean togglePause()
        {
            paused = !paused;
            if (paused)
                timePaused = DateTime.Now;
            else
                offset -= elapsedTime(timePaused);
            return paused;
        }

        private long elapsedTime(DateTime date)
        {
            return DateTime.Now.Ticks - date.Ticks;
        }

        private String suffix(DateTime date)
        {
            int day = date.Day;
            if (day > 10 && day < 20)
                return "th";
            switch (day % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

    }
}
