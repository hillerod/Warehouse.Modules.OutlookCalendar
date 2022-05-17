//using Bygdrift.Warehouse;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Extensions.Logging;
//using Module.Refine;
//using System.Threading.Tasks;

//namespace Module.AppFunctions
//{
//    public class TimerTrigger
//    {
//        public AppBase<Settings> App = new();

//        [FunctionName(nameof(TimerTriggerAsync))]
//        public async Task TimerTriggerAsync([TimerTrigger("%Setting--ScheduleExpression%")] TimerInfo myTimer, ILogger logger, int? take = null, bool moveFile = false)
//        {
//            //App.Log.Logger = logger;
//            //var ftpService = new Service.FTPService(App);

//            //var csvFiles = ftpService.GetData(take);

//            //var locationsRefinedCsv = await LocationsRefine.RefineAsync(App, true);
//            //var bookingsRefinedCsv = await BookingsRefine.RefineAsync(App, csvFiles, true);
//            //PartitionBookingsRefine.Refine(App, bookingsRefinedCsv, locationsRefinedCsv, 2, true);

//            //if (moveFile)
//            //    ftpService.MoveFolderContent("backup");
//        }
//    }
//}
