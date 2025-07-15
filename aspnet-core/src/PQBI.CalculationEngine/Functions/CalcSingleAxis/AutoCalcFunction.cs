namespace PQBI.CalculationEngine.Functions.CalcSingleAxis;

public class AutoCalcFunction : CalcFunctionBase
{
    public const string Auto_Function = "auto";

    public override string Alias => Auto_Function;

    public static bool IsAuto(string resolution) => resolution.StartsWith(Auto_Function, StringComparison.CurrentCultureIgnoreCase);

    public static bool TryToCalc(string str, int maxPoints, out int resolution)
    {
        var result = false;
        resolution = -1;

        str = str.ToLower();

        if (str.StartsWith(Auto_Function))
        {
            str = str.Remove(0, Auto_Function.Length + 1);
            str = str.Remove(str.Length - 1);
            var currentPoints = int.Parse(str);


            if (currentPoints < maxPoints)
            {
                resolution = currentPoints;
            }
            else
            {
                var res = (double)maxPoints / currentPoints;
                resolution = (int)Math.Ceiling(res);
            }

            result = true;
        }


        return result;
    }


    public static bool TryExtracMaxPoints(string str, out int maxPoints)
    {
        var result = false;
        maxPoints = -1;

        str = str.ToLower();

        if (str.StartsWith(Auto_Function))
        {
            str = str.Remove(0, Auto_Function.Length + 1); ;//remove auto(
            str = str.Remove(str.Length - 1);
            maxPoints = (int)Math.Floor(double.Parse(str));

            result = true;
        }


        return result;
    }


    public int CalcNumberForGroup(int currentPoints, int maxPoints)
    {
        if (currentPoints <= maxPoints)
        {
            return 1;
        }

        var tmp = (double)currentPoints / maxPoints;

        return (int)Math.Ceiling(tmp);

    }

    public int CalcNumInGroup(int maxPoints, IEnumerable<BasicValue> input)
    {
        var result = new List<BasicValue>();
        var count = input.Count();
        var numberInGroup = CalcNumberForGroup(input.Count(), maxPoints);

        return numberInGroup;

        //if (numberInGroup == 1)
        //{
        //    return input;
        //}

        //for (int i = 0; i < count; i += numberInGroup)
        //{
        //    //how to calculate what items should I take ?????
        //    result.Add(input.ElementAt(i));
        //}

        //return result;
    }
}