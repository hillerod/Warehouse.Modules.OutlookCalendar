using Bygdrift.Tools.CsvTool;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Module.Refine.PartitionBookings
{
    /// <summary>
    /// Slices bookings into timeslots like 8-9, 9-10..., and gives percent for usage
    /// </summary>
    public class Partitioning
    {
        public Dictionary<object, List<Timeslot>> Partitions { get; private set; }

        public Partitioning()
        {
            Partitions = new Dictionary<object, List<Timeslot>>();
        }

        public Partitioning(Csv csv, string headerNameFrom, string headerNameTo, string headerNameGroupId, string headerNameFactor)
        {
            Partitions = new Dictionary<object, List<Timeslot>>();
            if (!csv.TryGetColId(headerNameFrom, out int colFrom) || !csv.TryGetColId(headerNameTo, out int colTo) || !csv.TryGetColId(headerNameGroupId, out int colGroupId) || !csv.TryGetColId(headerNameFactor, out int colFactor))
                throw new Exception("Error in reading headerName. A programmer must take care of this issue.");

            foreach (var cols in csv.GetRowsRecords(false).Values)
            {
                if (cols.TryGetValue(colFrom, out object from) && cols.TryGetValue(colTo, out object to) && cols.TryGetValue(colGroupId, out object roomId) && cols.TryGetValue(colFactor, out object factorAsObject))
                {
                    double factor = 0;
                    if (factorAsObject is double)
                        factor = (double)factorAsObject;
                    if (factorAsObject is string && !string.IsNullOrEmpty((string)factorAsObject))
                        factor = double.Parse(factorAsObject.ToString(), CultureInfo.InvariantCulture);

                    AddPartition(roomId.ToString(), DateTime.Parse(from.ToString()), DateTime.Parse(to.ToString()), factor);
                }
                else
                {

                }
            }
        }

        public void AddGroupIdThatHasNoTimeslot(string groupId)
        {
            Partitions.Add(groupId, new List<Timeslot>());
        }

        public void AddPartition(object groupId, DateTime from, DateTime to, double factor)
        {
            if (Partitions.TryGetValue(groupId, out List<Timeslot> partitions))
                partitions.Add(new Timeslot(from, to, factor));
            else
                Partitions.Add(groupId, new List<Timeslot>() { new Timeslot(from, to, factor) });
        }

        public Dictionary<object, List<Timeslot>> GetPartitionsInTimeslices(DateTime fromDate, DateTime toDate, TimeSpan fromTime, TimeSpan toTime, short minuteSlice, bool includeWeekends, bool includeEmptyValues)
        {
            var res = new Dictionary<object, List<Timeslot>>();
            var dayAndTimePartition = EachDayAndTimePartition(fromDate, toDate, fromTime, toTime, minuteSlice, includeWeekends).ToList();
            foreach (var partition in Partitions.OrderBy(o => o.Key))
            {
                var newTimeslots = new List<Timeslot>();
                foreach (var (From, To) in dayAndTimePartition)
                {
                    var intersectingTimeslots = GetIntersectingTimeslots(partition.Value, From, To);
                    var mergedData = MergeData(intersectingTimeslots, From, To, includeEmptyValues);
                    newTimeslots.AddRange(mergedData);
                }
                res.Add(partition.Key, newTimeslots);
            }
            return res;
        }

        public Csv TimeslotsToCsv(Dictionary<object, List<Timeslot>> partitions)
        {
            var csv = new Csv("Id, Start, End, Minutes, Percent, Factor, GroupId");

            var r = 0;
            foreach (var partition in partitions)
            {
                var idMailPrefix = partition.Key.ToString().Replace("@HILLEROD.DK", "").ToUpper();
                foreach (var timeSlot in partition.Value)
                {
                    var idStart = timeSlot.Start.ToString("yyMMddhhmm");
                    csv.AddRecord(r, 1, idStart + "-" + idMailPrefix);
                    csv.AddRecord(r, 2, timeSlot.Start.ToString("s"));
                    csv.AddRecord(r, 3, timeSlot.End.ToString("s"));
                    csv.AddRecord(r, 4, timeSlot.Minutes);
                    csv.AddRecord(r, 5, timeSlot.Percent);
                    csv.AddRecord(r, 6, Math.Round(timeSlot.Factor, 3));
                    csv.AddRecord(r, 7, partition.Key);
                    r++;
                }

            }
            return csv;
        }

        private IEnumerable<Timeslot> MergeData(List<Timeslot> timeslots, DateTime from, DateTime to, bool includeEmptyValues)
        {
            if (timeslots != null && timeslots.Any())
            {
                var duration = timeslots.Sum(o => o.Duration.TotalMinutes);
                var factor = timeslots.Sum(o => o.Factor);
                var totalMinutes = (to - from).TotalMinutes;
                yield return new Timeslot(from, to, Math.Round(duration, 4), Math.Round(duration / totalMinutes, 4), factor);
            }
            else if (includeEmptyValues)
                yield return new Timeslot(from, to, 0, 0, 0);
        }

        private List<Timeslot> GetIntersectingTimeslots(List<Timeslot> timeslots, DateTime from, DateTime to)
        {
            var res = new List<Timeslot>();
            var datas = timeslots.Where(o => !(o.Start > to) && !(o.End < from)).OrderBy(o => o.Start);
            foreach (var data in datas)
            {
                var startsBefore = data.Start < from;
                var endsAfter = data.End > to;
                var fromRes = startsBefore ? from : data.Start;
                var toRes = endsAfter ? to : data.End;

                var resLength = toRes - fromRes;
                var length = to - from;
                var effectiveTimeFactor = resLength / length;

                var resData = res.FirstOrDefault(o => !(o.Start > toRes) && !(o.End < fromRes));
                if (resData != null)
                {
                    if (resData.Start > fromRes)
                        resData.Start = fromRes;
                    if (resData.End < toRes)
                        resData.End = toRes;
                    resData.Factor += data.Factor * effectiveTimeFactor;
                }
                else
                    res.Add(new Timeslot(fromRes, toRes, data.Factor * effectiveTimeFactor));
            }
            return res;
        }

        private IEnumerable<(DateTime From, DateTime To)> EachDayAndTimePartition(DateTime fromDate, DateTime toDate, TimeSpan fromTime, TimeSpan toTime, short minuteSlice, bool includeWeekends)
        {
            for (var day = fromDate.Date; day.Date <= toDate.Date; day = day.AddDays(1))
                if (includeWeekends || !includeWeekends && (int)day.DayOfWeek < 6)
                    for (var fromSlice = fromTime; fromSlice < toTime; fromSlice = fromSlice.Add(new TimeSpan(0, minuteSlice, 0)))
                    {
                        var toSlice = fromSlice.Add(new TimeSpan(0, minuteSlice, 0));
                        if (toSlice > toTime)
                            toSlice = toTime;

                        yield return (day.Add(fromSlice), day.Add(toSlice));
                    }
        }
    }
}
