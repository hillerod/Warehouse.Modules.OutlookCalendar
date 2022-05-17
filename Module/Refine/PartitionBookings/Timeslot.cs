using System;

namespace Module.Refine.PartitionBookings
{
    public class Timeslot
    {
        private DateTime _start;

        public Timeslot(DateTime from, DateTime to, double minutes, double percent, double factor)
        {
            Start = from;
            End = to;
            Minutes = minutes;
            Percent = percent;
            Factor = factor;
        }

        public DateTime Start
        {
            get { return _start; }
            set
            {
                _start = value;
                _duration = End - value;
            }
        }

        private DateTime _end;

        public DateTime End
        {
            get { return _end; }
            set
            {
                _end = value;
                _duration = value - Start;
            }
        }

        public double Factor { get; set; }

        private TimeSpan? _duration;

        public TimeSpan Duration
        {
            get { return (TimeSpan)(_duration ??= _duration = End - Start); }
        }

        public double Minutes { get; private set; }

        public double Percent { get; private set; }

        public Timeslot(DateTime from, DateTime to, double factor)
        {
            Start = from;
            End = to;
            Factor = factor;
        }
    }
}
