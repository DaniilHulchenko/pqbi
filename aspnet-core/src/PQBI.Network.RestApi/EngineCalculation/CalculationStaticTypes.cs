using PQBI.PQS.CalcEngine;

namespace PQBI.Network.RestApi.EngineCalculation;

public static class CalculationStaticTypes
{

    public static CustomParameterType GetCustomParameterType(string type)
    {
        if (Enum.TryParse(type, true, out CustomParameterType columnParameterType))
        {
            return columnParameterType;
        }

        throw new PQBIException(new PropertyServiceError("type", $"Is not part of {nameof(CustomParameterType)}"));
    }

    public static TrendWidgetParameterType GetCustomParameterTrendType(string type)
    {
        if (Enum.TryParse(type, true, out TrendWidgetParameterType columnParameterType))
        {
            return columnParameterType;
        }

        throw new PQBIException(new PropertyServiceError("type", $"Is not part of {nameof(TrendWidgetParameterType)}"));
    }

    public static TableWidgetParameterType GetTableWidgetParameterType(string type)
    {
        if (Enum.TryParse(type, true, out TableWidgetParameterType columnParameterType))
        {
            return columnParameterType;
        }

        throw new PQBIException(new PropertyServiceError("type", $"Is not part of {nameof(TableWidgetParameterType)}"));
    }

    public static ParameterListItemType GetParameterListType(string type)
    {
        if (Enum.TryParse(type, true, out ParameterListItemType parameterListItemType))
        {
            return parameterListItemType;
        }

        throw new PQBIException(new PropertyServiceError("type", $"Is not part of {nameof(ParameterListItemType)}"));
    }

}
