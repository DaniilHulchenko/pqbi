using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PQBI.CustomParameters;
using PQBI.Network.RestApi.EngineCalculation;
using PQBI.PQS.CalcEngine;
using PQBI.Trace;
using PQS.CommonUI.Data;
using PQS.CommonUI.Enums;
using System.DirectoryServices.ActiveDirectory;

namespace PQBI.UnitTests
{
    public class LogWatcherTest
    {


        [Fact]
        public void GetLegendname_Test()
        {
            var baseParameter = ForTest();


            //var param = CreateParameterComponentHelper.AddFeederSlim(baseParameter,"1");

            //var dataSource = new MeasurementParameterDataSource(serverURL: null, compID: Guid.NewGuid(), parameter: param, quantity: QuantityType.Max, parameterDescriptor: null, color: null, isVisible: true);
            //var name = dataSource.GetLegendName();


        }

        public static BaseParameter ForTest()
        {
            var json = """
{
  "STDPQSParametersList": "[{\"type\":\"LOGICAL\",\"name\":\"bp1\",\"aggregationFunction\":\"Avg\",\"Operator\":\"Mul(2)\",\"quantity\":\"QAVG\",\"group\":\"RMS\",\"resolution\":\"IS1MIN\",\"phase\":\"UV1N\",\"base\":\"BHCYC\",\"id\":\"6nCqj\"}]",
}



""";


            var customParameter = JsonConvert.DeserializeObject<CustomParameter>(json);
            var parameterList = JsonConvert.DeserializeObject<BaseParameter[]>(customParameter.CustomBaseDataList);


            return parameterList.First();

        }


        [Fact]
        public void SingleLog()
        {

            ILogWatcherService watcher = new LogWatcherService(new LogWatcherConfig { Capacity = 1 });

            watcher.AddLog("xxxxx");
            var logs = watcher.Peek(1);

            Assert.Equal(1, logs.Length);

        }



        [Theory]
        [MemberData(nameof(SimpleTestData))]
        public void SimpleMultipalLogs(int expected, int capacity, params string[] logsToAdd)
        {
            var watcher = new LogWatcherService(new LogWatcherConfig { Capacity = capacity });

            foreach (var log in logsToAdd)
            {
                watcher.AddLog(log);
            }

            var logs = watcher.Peek(expected);

            Assert.Equal(expected, logs.Length);
            foreach (var log in logs)
            {
                Assert.NotNull(log);
            }
        }


        [Fact]
        public void RollePeekTest()
        {
            var watcher = new LogWatcherService(new LogWatcherConfig { Capacity = 3 });

            var logsToAdd = new[] { "111", "222", "333", "444" };

            foreach (var log in logsToAdd)
            {
                watcher.AddLog(log);

            }

            var logs = watcher.Peek(3);
            Assert.Equal("444", logs[0]);
            Assert.Equal("333", logs[1]);
            Assert.Equal("222", logs[2]);
            Assert.Equal(3, logs.Length);
        }


        public static IEnumerable<object[]> SimpleTestData()
        {
            yield return new object[] { 1, 10, new string[] { "xxx" } };
            yield return new object[] { 2, 10, new string[] { "xxx", "xxx" } };
            yield return new object[] { 6, 10, new string[] { "xxx", "xxx", "xxx", "xxx", "xxx", "xxx" } };

        }
    }


    public class LogWatcherConfigWrapper : IOptions<LogWatcherConfig>
    {
        private readonly LogWatcherConfig _config;

        public LogWatcherConfigWrapper(LogWatcherConfig config)
        {
            _config = config;
        }
        public LogWatcherConfig Value => _config;
    }
}