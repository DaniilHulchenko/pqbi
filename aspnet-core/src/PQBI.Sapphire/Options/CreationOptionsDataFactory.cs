using PQBI.PQS;
using PQBI.Sapphire.Options;
using PQS.Data.Measurements.CustomParameter;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public static class CreationOptionsDataFactory
{


    public static StaticTreeNode CreateOptionDtos()
    {
        var tree = new StaticTreeNode { Value = StaticTreeNode.RootLabel , Description = StaticTreeNode.RootLabel };
        List<CustomCalculationBaseInfo> customCalcBaseInfoList = new List<CustomCalculationBaseInfo>();
        var logicalDataGenerator = new CreationLogicalOptions();
        var channelDataGenerator = new CreationChannelOptions();

        var logicalData = logicalDataGenerator.CreateDataAsync(customCalcBaseInfoList);
        tree.Children.Add(logicalData);


        var channelData = channelDataGenerator.CreateDataAsync(customCalcBaseInfoList);
        tree.Children.Add(channelData);


        return tree;
    }
}
