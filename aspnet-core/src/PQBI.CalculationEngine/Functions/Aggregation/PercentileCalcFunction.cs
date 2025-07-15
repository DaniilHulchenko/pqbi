using System.Text.RegularExpressions;

namespace PQBI.CalculationEngine.Functions.Aggregation;

public interface ICalcEngineParameterValidation
{
    bool TryExtracParameter(string input, out double parameter);
}

public class PercentileCalcFunction : SingleCalculationFunction, ICalcEngineParameterValidation
{
    public const string Percentile_Function = "percentile";

    public override string Alias => Percentile_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        var sortedSequence = input.Data.Where(x=> x.Value is not null).OrderBy(x => x).ToArray();

        double rank = parameter / 100.0 * (sortedSequence.Length - 1) + 1;
        int integerRank = (int)rank;
        double fractionalRank = rank - integerRank;

        if (integerRank >= sortedSequence.Length)
        {
            //Todo refactored
            return new BasicValue(sortedSequence.Last().Value,PqbiDataValueStatus.Ok);
            //return sortedSequence.Last();
        }

        if (integerRank == 0)
        {
            //Todo refactored
            return new BasicValue(sortedSequence.First().Value, PqbiDataValueStatus.Ok);
            //return sortedSequence.First();
        }

        BasicValue lowerValue = sortedSequence[integerRank - 1];
        BasicValue upperValue = sortedSequence[integerRank];

        var result =  lowerValue.Value + (upperValue.Value - lowerValue.Value) * fractionalRank;
        return new BasicValue(result, PqbiDataValueStatus.Ok);
        //return result;
    }

    public bool TryExtracParameter(string funWithparameter, out double parameter)
    {
        var result = true;
        parameter = 0;

        var pattern = @"\((\d+(\.\d+)?)\)";
        var match = Regex.Match(funWithparameter, pattern);
        var numberStr = match.Groups[1].Value;
        
        if (string.IsNullOrEmpty(numberStr))
        {
            return false;
        }

        parameter = double.Parse(numberStr);
        return true;

    }
}
