using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.Refine.PartitionBookings;
using System;
using System.Linq;

namespace ModuleTests.Refine
{
    [TestClass]
    public class PartitioningRefineTest
    {
        [TestMethod]
        public void GetDataPerHourTest()
        {
            Partitioning trim = GetData();

            var res = trim.GetPartitionsInTimeslices(new DateTime(2021, 1, 1), new DateTime(2021, 1, 2), new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), 60, false, true).ToList();
            Assert.IsTrue(!res.Any(o => o.Key.Equals("A") && o.Value.Any(p => p.Start == new DateTime(2021, 1, 2, 7, 0, 0))));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && !o.Value.Any(p => p.Start == new DateTime(2021, 1, 1, 7, 0, 0))));
            Assert.IsTrue(!res.Any(o => o.Key.Equals("B") && o.Value.Any(p => p.Start == new DateTime(2021, 1, 2, 7, 0, 0))));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && !o.Value.Any(p => p.Start == new DateTime(2021, 1, 1, 12, 0, 0))));

            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 8, 0, 0)).Minutes == 60));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 9, 0, 0)).Minutes == 0));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 11, 0, 0)).Minutes == 30));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 11, 0, 0)).Percent == .5));
            Assert.IsTrue(!res.Any(o => o.Key.Equals("B") && o.Value.Any(p => p.Start == new DateTime(2021, 1, 2, 8, 0, 0))));  //Weekends should be excluded in this test
        }

        [TestMethod]
        public void GetDataPerSecondHourTest()
        {
            Partitioning trim = GetData();
            var res = trim.GetPartitionsInTimeslices(new DateTime(2021, 1, 1), new DateTime(2021, 1, 2), new TimeSpan(8, 0, 0), new TimeSpan(13, 0, 0), 120, false, true).ToList();

            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && !o.Value.Any(p => p.Start == new DateTime(2021, 1, 1, 7, 0, 0))));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 8, 0, 0)).End.Hour == 10));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 10, 0, 0)).End.Hour == 12));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && o.Value.Single(p => p.Start == new DateTime(2021, 1, 1, 12, 0, 0)).End.Hour == 13));
            Assert.IsTrue(res.Any(o => o.Key.Equals("B") && !o.Value.Any(p => p.Start == new DateTime(2021, 1, 1, 13, 0, 0))));
        }

        private Partitioning GetData()
        {
            var trim = new Partitioning();
            trim.AddGroupIdThatHasNoTimeslot("A");
            trim.AddPartition("B", new DateTime(2021, 1, 1, 8, 0, 0), new DateTime(2021, 1, 1, 9, 0, 0), 1);
            trim.AddPartition("B", new DateTime(2021, 1, 1, 10, 0, 0), new DateTime(2021, 1, 1, 11, 0, 0), 1);
            trim.AddPartition("B", new DateTime(2021, 1, 1, 10, 30, 0), new DateTime(2021, 1, 1, 11, 30, 0), 1);  //Overleap on purpous
            trim.AddPartition("B", new DateTime(2021, 1, 2, 8, 0, 0), new DateTime(2021, 1, 2, 11, 0, 0), 1);  //A saturday
            return trim;
        }
    }
}