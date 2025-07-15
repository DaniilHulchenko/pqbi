using PQBI.CalculationEngine.Functions.CalcSingleAxis;
using PQS.Data.Common.Attributes;
using System.Linq;
using System.Text.RegularExpressions;

namespace PQBI.CalculationEngine.Functions;


public class GroupByFunctionInput : SingleAxisInput
{
    public int ResolutionInSeconds { get; set; }
    public int SyncInSeconds { get; set; }
    public int NumInGroup { get; set; }

    public GroupByFunctionInput(IEnumerable<BasicValue> data, int numInGroup)
    {
        Data = data;
        NumInGroup = numInGroup;
    }

    public GroupByFunctionInput(IEnumerable<BasicValue> data, int resolutionInSecond, int syncInSeconds)
    {
        Data = data;
        ResolutionInSeconds = resolutionInSecond;
        SyncInSeconds = syncInSeconds;

        NumInGroup = ResolutionInSeconds / SyncInSeconds;        
    }
}


public class GroupByCalcFunction : CalcFunctionBase
{
    public const string GroupBy_Function = "groupby";

    public const int Single_Resolution = -1;

    public override string Alias => GroupBy_Function;

    public List<BasicValue[]> Calc(GroupByFunctionInput input)
    {
        var result = new List<BasicValue[]>();

        if (input.ResolutionInSeconds == Single_Resolution)
        {

            result.Add(input.Data.ToArray());
            return result;
        }

        //int resolutionInSeconds = input.ResolutionInSeconds;
        //int syncInSeconds = input.SyncInSeconds;
        //int gVec = resolutionInSeconds / syncInSeconds;
        int gVec = input.NumInGroup;

        var data = input.Data as BasicValue[] ?? input.Data.ToArray();
        int dataCount = data.Length;

        BasicValue[] buffer = new BasicValue[gVec];
        int bufferIndex = 0;

        for (int index = 0; index < dataCount; index++)
        {
            buffer[bufferIndex++] = data[index];

            if (bufferIndex == gVec)
            {
                result.Add(buffer);
                buffer = new BasicValue[gVec];
                bufferIndex = 0;
            }
        }

        if (bufferIndex > 0)
        {
            result.Add(buffer.Take(bufferIndex).ToArray());
        }

        return result;
    }



    private static int ConvertToMinute(int number, string unit)
    {
        unit = unit.ToLower();


        if (unit.StartsWith("m"))
        {
            return number;
        }

        if (unit.StartsWith("h"))
        {
            return number * 60;
        }

        throw new ArgumentException("Theprefix can be only S,M,H");
    }


    public static string ConvertToResolution(double periodInMinutes)
    {

        int days = (int)(periodInMinutes / (60 * 24)); // 1 day = 24 hours = 1440 minutes
        int remainingMinutesAfterDays = (int)(periodInMinutes % (60 * 24));

        int hours = remainingMinutesAfterDays / 60; // 1 hour = 60 minutes
        int minutes = remainingMinutesAfterDays % 60;

        if (days == 1)
        {
            return "IS1HOUR";
        }


        if (days > 0)
        {
            return "IS1DAY";
        }

        if (hours > 0)
        {
            return "IS1HOUR";
        }

        if (minutes > 0)
        {
            return "IS1MIN";
        }

        throw new ArgumentException("The prefix can be only D,M,M");
    }


    public static int ConvertToSecond(int number, string unit)
    {
        unit = unit.ToLower();

        if (unit.StartsWith("s"))
        {
            return number;
        }

        if (unit.StartsWith("m"))
        {
            return number * 60;
        }

        if (unit.StartsWith("h"))
        {
            return number * 60 * 60;
        }

        if (unit.StartsWith("d"))
        {
            return number * 60 * 60 * 24;
        }

        throw new ArgumentException("The prefix can be only S,M,H");
    }





    public static (int, string) ParsePeriod(string input)
    {
        var match = Regex.Match(input, @"isn?(\d+)(\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            int number = int.Parse(match.Groups[1].Value);
            string word = match.Groups[2].Value;
            return (number, word);
        }

        throw new ArgumentException("Input string is not in the expected format.");
    }

    public static bool IsSingleResolution(string input) => input.Equals(Single_Resolution.ToString());
    //public static bool IsSingleResolution(string input) => input.Equals(Single_Resolution);

    public static int ParseAndConvertToSecond(string input)
    {
        if (IsSingleResolution(input))
        {
            return Single_Resolution;
        }

        var (number, word) = ParsePeriod(input);
        var seconds = ConvertToSecond(number, word);

        return seconds;
    }

    //TODO
    public static bool TryParseAndConvertToSecond(string input, out int seconds)
    {
        seconds = 0;
        var result = false;

        try
        {
            var (number, word) = ParsePeriod(input);
            seconds = ConvertToSecond(number, word);
        }
        catch
        {
            result = false;
        }

        return result;
    }
}
