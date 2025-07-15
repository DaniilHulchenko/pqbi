using FluentAssertions;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Functions.Aggregation;
using PQBI.Network.RestApi;
using System.Data;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
namespace PQBI.UnitTests;

public class XUnitLogger<T> : ILogger<T>, IDisposable
{
    private readonly ITestOutputHelper _output;

    public XUnitLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state) => this;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine(formatter(state, exception));
    }

    public void Dispose() { }
}

public class FunctionEngineTest
{
    private readonly ILogger<FunctionEngineTest> _logger;

    public FunctionEngineTest(ITestOutputHelper output)
    {
        _logger = new XUnitLogger<FunctionEngineTest>(output);
    }


    [Fact]
    public async Task PercentileCalcFunction_ReturnFalse()
    {
        var function = new PercentileCalcFunction();
        var nums = Enumerable.Range(0, 100).Select(x => (double)x).ToArray();

        function.TryExtracParameter($"{PercentileCalcFunction.Percentile_Function}(ccc)", out _).Should().BeFalse();

    }

    [Fact]
    public async Task PercentileCalcFunction_ReturnTrue()
    {
        const double expected = 10;
        var function = new PercentileCalcFunction();
        var nums = Enumerable.Range(0, 100).Select(x => (double)x).ToArray();

        function.TryExtracParameter($"{PercentileCalcFunction.Percentile_Function}({expected})", out var result).Should().BeTrue();

    }

    [Fact]
    public async Task GroupByCalcFunction_ReturnTrue()
    {
        const double expected = 10;
        var function = new GroupByCalcFunction();
        var nums = Enumerable.Range(0, 1000000).Select(x => (double?)x).ToArray();
        var input = new GroupByFunctionInput
        {
            Data = nums.Select(x=> new BasicValue(x,PqbiDataValueStatus.Ok)),
            ResolutionInSeconds = 600,
            SyncInSeconds = 60,
        };

        LoggerStopwatch logger = null;
        using (logger =  PqbiStopwatch.AnchorAsync($"{nameof(GroupByCalcFunction_ReturnTrue)}", _logger))
        {
            var ptr = function.Calc(input);
        }

    }

    [Fact]
    public async Task GetFullGraphTesr()
    {
        const double expected = 10;
        var function = new GroupByCalcFunction();
        var nums = Enumerable.Range(0, 1000000).Select(x => (double?)x).ToArray();
        var input = new GroupByFunctionInput
        {
            Data = nums.Select(x => new BasicValue(x, PqbiDataValueStatus.Ok)),
            ResolutionInSeconds = 600,
            SyncInSeconds = 60,
        };

        LoggerStopwatch logger = null;
        using (logger = PqbiStopwatch.AnchorAsync($"{nameof(GroupByCalcFunction_ReturnTrue)}", _logger))
        {
            var ptr = function.Calc(input);
        }

    }

}
