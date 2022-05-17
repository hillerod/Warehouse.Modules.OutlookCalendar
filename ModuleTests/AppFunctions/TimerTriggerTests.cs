using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleTests.AppFunctions
{
    [TestClass]
    public class TimerTriggerTests
    {
        //public async Task TimerTrigger()
        //{
        //    Mock<ILogger> loggerMock = new();
        //    var function = new Module.AppFunctions.TimerTrigger();
        //    await function.TimerTriggerAsync(default(TimerInfo), loggerMock.Object);
        //    var errors = function.App.Log.GetErrorsAndCriticals();
        //    Assert.IsTrue(errors.Count() == 0);
        //}
    }
}
