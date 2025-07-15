using PQBI.PQS;
using PQS.Data.Measurements;
using System.Text.RegularExpressions;
using PQS.Data.Measurements.StandardParameter;
using PQS.Translator;
using Abp.UI;
using PQBI.Requests;
//using PQS.Data.Measurements.StandardParameter;

namespace PQBI.Network.RestApi;

public interface IFeederChannelTredBuilder
{
    Task<DynamicTreeNode> GetLogicalOrChannelTreeAsync(params string[] parameters);
}

public class FeederChannelTredBuilder : IFeederChannelTredBuilder
{
    private const string Feeder_Name = "FEEDER";
    private const string Channel_Name = "CH";
    private const string STD_Name = "STD";
    private const string MULTI_STD_NAME = "MULTI_STD";
    private const string MULTISTD_NAME = "MULTISTD";


    public async Task<DynamicTreeNode> GetLogicalOrChannelTreeAsync(params string[] parameters)
    {
        var tree = new DynamicTreeNode { Value = DynamicTreeNode.RootLabel };

        var logicalTree = new DynamicTreeNode { Value = DynamicTreeNode.LogicalLabel };
        var channelTree = new DynamicTreeNode { Value = DynamicTreeNode.ChannelLabel };

        tree.Children.Add(logicalTree);
        tree.Children.Add(channelTree);

        foreach (var parameter in parameters)
        {
            var parameterName = parameter.Split('#').FirstOrDefault();

            if (parameterName.Contains("SRC"))
            {
                parameterName = Regex.Replace(parameterName, "SRC", Feeder_Name);
            }

            if (parameterName.Contains(Feeder_Name))
            {
                CreateFeederTreeFromParameter(parameterName, logicalTree);
            }
            else
            {
                if (parameterName.Contains(Channel_Name))
                {
                    CreateChannelTreeFromParameter(parameterName, channelTree);
                }
            }
        }

        return tree;
    }


    private void CreateFeederTreeFromParameter(string parameterName, DynamicTreeNode tree)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        MeasurementParameterBase msrParm = MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(parameterName);

        var paramInfo = new ParameterPartDto2
        {
            ParameterName = parameterName,

            Group = msrParm.GetGroupName(),
            GroupDescription = ((StandardMeasurementParameter)msrParm).Group.Description(),

            CalculationBase = msrParm.CalculationBaseClass.CalculationBaseEnum.ToString(),
            CalculationBaseDescription = msrParm.CalculationBaseClass.CalculationBaseEnum.Description(),


            QuantityEnum = msrParm.Quantity.Description(),
        };



        if (parameterName.Contains(Feeder_Name))
        {
            var isMulti = false;
            if (parameterName.Contains("MULTI"))
            {
                parameterName = Regex.Replace(parameterName, MULTI_STD_NAME, MULTISTD_NAME);
                isMulti = true;
            }

            var pattern = @"(FEEDER_\d+)";
            var regex = new Regex(pattern);
            var match = regex.Match(parameterName);
            var feederAlias = match.Value;

            int lastIndex = parameterName.LastIndexOf('_');
            parameterName = parameterName.Remove(lastIndex, 1);

            var currentNode = tree;

            parameterName = MoveFeederToBeginning(parameterName);
            parameterName = SwapBetweenBaseAndPhase(parameterName);
            var @params = parameterName
                    .Split('_')
                    .Select((value, index) => FeederTreeMapper(isMulti, value, index, paramInfo))
                    .ToList();

            @params[0] = (feederAlias, @params[0].description);

            if (isMulti)
            {
                @params[1] = (MULTI_STD_NAME, MULTI_STD_NAME);
            }

            foreach (var param in @params)
            {
                var childNode = currentNode.Children.FirstOrDefault(n => n.Value == param.value);

                if (childNode == null)
                {
                    childNode = new DynamicTreeNode { Value = param.value, Description = param.description };
                    currentNode.Children.Add(childNode);
                }

                currentNode = childNode;
            }
        }
    }

    private (string value, string description) FeederTreeMapper(bool isMulti, string value, int index, ParameterPartDto2 paramInfo)
    {
        var result = (value, value);

        if (index == 2) //GROUP
        {
            result = (value, paramInfo.GroupDescription);
        }
        else
        {
            if (isMulti == false)
            {
                if (index == 3) //Phase
                {
                    result = (value, value);
                }

                if (index == 4) //Base
                {
                    result = (value, paramInfo.CalculationBaseDescription);
                }
            }
            else
            {
                if (index == 4)
                {
                    result = (value, value);
                }

                if (index == 5)
                {
                    result = (value, paramInfo.CalculationBaseDescription);
                }
            }
        }

        return result;
    }

    private (string value, string description) ChannelTreeMapper(bool isMulti, string value, int index, ParameterPartDto2 paramInfo)
    {
        var result = (value, value);

        if (index == 1) //GROUP
        {
            result = (value, paramInfo.GroupDescription);
        }
        else
        {
            if (isMulti == false)
            {
                if (index == 3) //Base
                {
                    result = (value, paramInfo.CalculationBaseDescription);
                }
            }
            else
            {
                if (index == 4) //Base
                {
                    result = (value, paramInfo.CalculationBaseDescription);
                }
            }
        }

        return result;
    }

    private void CreateChannelTreeFromParameter(string parameterName, DynamicTreeNode tree)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        MeasurementParameterBase msrParm = MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(parameterName);

        var paramInfo = new ParameterPartDto2
        {
            ParameterName = parameterName,

            Group = msrParm.GetGroupName(),
            GroupDescription = ((StandardMeasurementParameter)msrParm).Group.Description(),

            CalculationBase = msrParm.CalculationBaseClass.CalculationBaseEnum.ToString(),
            CalculationBaseDescription = msrParm.CalculationBaseClass.CalculationBaseEnum.Description(),


            QuantityEnum = msrParm.Quantity.Description(),
        };

        if (parameterName.Contains(Channel_Name))
        {
            var isMulti = false;
            if (parameterName.Contains("MULTI"))
            {
                parameterName = Regex.Replace(parameterName, MULTI_STD_NAME, MULTISTD_NAME);
                isMulti = true;
            }

            var pattern = @"(CH_\d+)";
            var regex = new Regex(pattern);
            var match = regex.Match(parameterName);

            var elemetAlias = match.Value;
            var newValue = elemetAlias.Replace("_", string.Empty);


            int lastIndex = parameterName.LastIndexOf('_');
            parameterName = parameterName.Remove(lastIndex, 1);

            parameterName = SwapBetweenBaseAndPhase(parameterName);

            var @params = parameterName
                  .Split('_')
                  .Select((value, index) => ChannelTreeMapper(isMulti, value, index, paramInfo))
                  .ToList();
            var currentNode = tree;

            if (isMulti)
            {
                @params[0] = (MULTI_STD_NAME, MULTI_STD_NAME);
            }


            for (int index = 0; index < @params.Count;)
            {
                var param = @params[index];
                if (param.value.Equals(newValue))
                {
                    param = (elemetAlias, elemetAlias);
                }
                var childNode = currentNode.Children.FirstOrDefault(n => n.Value == param.value);

                if (childNode == null)
                {
                    childNode = new DynamicTreeNode { Value = param.value, Description = param.description };
                    currentNode.Children.Add(childNode);
                }

                currentNode = childNode;
                index++;
            }
        }
    }



    public static string SwapBetweenBaseAndPhase(string str)
    {
        if (str == null) throw new ArgumentNullException(nameof(str));

        var parts = str.Split('_');

        if (parts.Length < 2) return str;

        var temp = parts[^1];
        parts[^1] = parts[^2];
        parts[^2] = temp;

        return string.Join('_', parts);
    }

    public static string MoveFeederToBeginning(string str)
    {
        if (str == null) throw new ArgumentNullException(nameof(str));

        var pattern = @"(FEEDER\d+)";
        var regex = new Regex(pattern);

        var match = regex.Match(str);
        if (!match.Success) return str;

        var feederPart = match.Value;
        var remainingPart = regex.Replace(str, "").TrimEnd('_');

        return $"{feederPart}_{remainingPart}";
    }
}
