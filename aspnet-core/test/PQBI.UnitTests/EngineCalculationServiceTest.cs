//using FluentAssertions;
//using PQBI.Network.RestApi.EngineCalculation;
//using static PQBI.Network.RestApi.EngineCalculation.EngineCalculationService;

//namespace PQBI.UnitTests
//{
//    public class EngineCalculationServiceTest
//    {
//        [Fact]
//        public async Task Test__AvgInterpolation()
//        {
//            const int numberOfPOints = 7;
//            const int customResolutionSeconds = 10;
//            var data = new double[] { 2, 7, 19 }; //==> 
//            var start = new DateTime(2025, 12, 1, 8, 0, 0);
//            var seconds = 3;

//            var result = EngineCalculationService.CalcInterpolation(customResolutionSeconds, start, data, seconds, numberOfPOints, InterpolationType.Avg).ToArray();

//            result[0].Value.Should().Be(data.First()); // 10 sec
//            result[1].Value.Should().Be(0.7 * 2 + 0.3 * 7); // 13  ==> 0.7*2 +0.3*7
//            result[2].Value.Should().Be(0.4 * 2 + 0.6 * 7);
//            result[3].Value.Should().Be(0.1 * 2 + 0.9 * 7);
//            result[4].Value.Should().Be(0.8 * 7 + 0.2 * 19); //9.4
//            result[5].Value.Should().Be(0.5 * 7 + 0.5 * 19); //13
//            result[6].Value.Should().Be(0.2 * 7 + 0.8 * 19); //16.6

//            //Should be 7 points!!!!!!!!

//        }

//        [Fact]
//        public async Task Test__AvgInterpolation2()
//        {
//            const int numberOfPOints = 5;
//            const int customResolutionSeconds = 30;
//            var data = new double[] { 2, 7, 19 }; //==> 
//            var start = new DateTime(2025, 12, 1, 8, 0, 0);
//            var seconds = 15;

//            var result = EngineCalculationService.CalcInterpolation(customResolutionSeconds, start, data, seconds, numberOfPOints, InterpolationType.Avg).ToArray();

//            result[0].Value.Should().Be(2); // 10 sec
//            result[1].Value.Should().Be(4.5); // 13  ==> 0.7*2 +0.3*7
//            result[2].Value.Should().Be(7);
//            result[3].Value.Should().Be(13);
//            result[4].Value.Should().Be(19); //9.4
//        }




//        [Fact]
//        public async Task Test__MinInterpolation()
//        {
//            const int numberOfPOints = 5;
//            const int customResolutionSeconds = 30;
//            var data = new double[] { 2, 7, 19 }; //==> 
//            var start = new DateTime(2025, 12, 1, 8, 0, 0);
//            var seconds = 15;

//            var result = EngineCalculationService.CalcInterpolation(customResolutionSeconds, start, data, seconds, numberOfPOints, InterpolationType.Min).ToArray();

//            result[0].Value.Should().Be(2); // 10 sec
//            result[1].Value.Should().Be(2); // 13  ==> 0.7*2 +0.3*7
//            result[2].Value.Should().Be(7);
//            result[3].Value.Should().Be(7);
//            result[4].Value.Should().Be(19); //9.4


//            //Should be 7 points!!!!!!!!

//        }


//        [Fact]
//        public async Task Test__MinInterpolation2()
//        {
//            const int numberOfPOints = 7;
//            const int customResolutionSeconds = 30;
//            var data = new double[] { 2, 7, 19, 25, 30, 7 }; //==> 
//            var start = new DateTime(2025, 12, 1, 8, 0, 0);
//            var seconds = 25;

//            var result = EngineCalculationService.CalcInterpolation(customResolutionSeconds, start, data, seconds, numberOfPOints, InterpolationType.Min).ToArray();

//            result[0].Value.Should().Be(2); // 10 sec
//            result[1].Value.Should().Be(2); // 13  ==> 0.7*2 +0.3*7
//            result[2].Value.Should().Be(7);
//            result[3].Value.Should().Be(19);
//            result[4].Value.Should().Be(25);
//            result[5].Value.Should().Be(30);
//            result[6].Value.Should().Be(7);


//            //Should be 7 points!!!!!!!!
//        }


//    }
//}
