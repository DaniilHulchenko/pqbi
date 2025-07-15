using PQBI.BarChartWidgetConfigurations;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Abp.Localization;
using Abp.Localization.Sources;
using System.Linq;
using Abp.Collections.Extensions;
using PQBI.DataExporting.Excel.MiniExcel;
using PQBI.DataImporting.Excel;
using PQBI.BarChartWidgetConfigurations.Importing.Dto;

namespace PQBI.BarChartWidgetConfigurations
{
    public class BarChartWidgetConfigurationListExcelDataReader(ILocalizationManager localizationManager)
        : MiniExcelExcelImporterBase<ImportBarChartWidgetConfigurationDto>, IExcelDataReader<ImportBarChartWidgetConfigurationDto>
    {
        private readonly ILocalizationSource _localizationSource = localizationManager.GetSource(PQBIConsts.LocalizationSourceName);

    public List<ImportBarChartWidgetConfigurationDto> GetEntitiesFromExcel(byte[] fileBytes)
    {
        return ProcessExcelFile(fileBytes, ProcessExcelRow);
    }

    private ImportBarChartWidgetConfigurationDto ProcessExcelRow(dynamic row)
    {

        var exceptionMessage = new StringBuilder();
        var barChartWidgetConfiguration = new ImportBarChartWidgetConfigurationDto();

        try
        {
            barChartWidgetConfiguration.Components = GetRequiredValueFromRowOrNull(row, nameof(barChartWidgetConfiguration.Components), exceptionMessage);
            barChartWidgetConfiguration.Events = GetRequiredValueFromRowOrNull(row, nameof(barChartWidgetConfiguration.Events), exceptionMessage);
            barChartWidgetConfiguration.DateRange = GetOptionalValueFromRowOrNull<string>(row, nameof(barChartWidgetConfiguration.DateRange), exceptionMessage);

        }
        catch (Exception exception)
        {
            barChartWidgetConfiguration.Exception = exception.Message;
        }

        return barChartWidgetConfiguration;
    }

    private string GetRequiredValueFromRowOrNull(
        dynamic row,
        string columnName,
        StringBuilder exceptionMessage)
    {
        var cellValue = (row as ExpandoObject).GetOrDefault(columnName)?.ToString();
        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue))
        {
            return cellValue;
        }

        exceptionMessage.Append(GetLocalizedExceptionMessagePart(columnName));
        return null;
    }

    private object GetOptionalValueFromRowOrNull<T>(dynamic row, string columnName, StringBuilder exceptionMessage)
    {
        var cellValue = (row as ExpandoObject).GetOrDefault(columnName)?.ToString();
        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue))
        {
            return cellValue;
        }

        exceptionMessage.Append(GetLocalizedExceptionMessagePart(columnName));
        return default(T);
    }

    private string GetLocalizedExceptionMessagePart(string parameter)
    {
        return _localizationSource.GetString("{0}IsInvalid", _localizationSource.GetString(parameter)) + "; ";
    }

    // Possible types: Int32, Long, Guid, String
    // PowerTools:ConvertToRequiredPrimaryKey
    private object ConvertToRequiredPrimaryKey<T>(string value)
    {
        return typeof(T).Name switch
        {
            "Int32" => Convert.ToInt32(value),
            "Long" => Convert.ToInt64(value),
            "Guid" => Guid.Parse(value),
            "String" => value,
            _ => default(T)
        };
    }

}
}