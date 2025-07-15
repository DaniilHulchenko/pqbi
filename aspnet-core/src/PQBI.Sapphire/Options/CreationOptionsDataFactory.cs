using PQBI.PQS;
using PQBI.Sapphire.Options;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public static class CreationOptionsDataFactory
{


    public static StaticTreeNode CreateOptionDtos()
    {
        var tree = new StaticTreeNode { Value = StaticTreeNode.RootLabel , Description = StaticTreeNode.RootLabel };

        var logicalDataGenerator = new CreationLogicalOptions();
        var channelDataGenerator = new CreationChannelOptions();

        var logicalData = logicalDataGenerator.CreateDataAsync();
        tree.Children.Add(logicalData);


        var channelData = channelDataGenerator.CreateDataAsync();
        tree.Children.Add(channelData);


        return tree;
    }
}
