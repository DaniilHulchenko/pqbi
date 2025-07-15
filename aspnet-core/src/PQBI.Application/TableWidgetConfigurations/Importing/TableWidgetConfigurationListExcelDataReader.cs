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
using PQBI.TableWidgetConfigurations.Importing.Dto;

namespace PQBI.TableWidgetConfigurations
{
    public class TableWidgetConfigurationListExcelDataReader(ILocalizationManager localizationManager)
        : MiniExcelExcelImporterBase<ImportTableWidgetConfigurationDto>, IExcelDataReader<ImportTableWidgetConfigurationDto>
    {
        private readonly ILocalizationSource _localizationSource = localizationManager.GetSource(PQBIConsts.LocalizationSourceName);

    public List<ImportTableWidgetConfigurationDto> GetEntitiesFromExcel(byte[] fileBytes)
    {
        return ProcessExcelFile(fileBytes, ProcessExcelRow);
    }

    private ImportTableWidgetConfigurationDto ProcessExcelRow(dynamic row)
    {

        var exceptionMessage = new StringBuilder();
        var tableWidgetConfiguration = new ImportTableWidgetConfigurationDto();

        try
        {
            tableWidgetConfiguration.Configuration = GetRequiredValueFromRowOrNull(row, nameof(tableWidgetConfiguration.Configuration), exceptionMessage);
            tableWidgetConfiguration.Components = GetRequiredValueFromRowOrNull(row, nameof(tableWidgetConfiguration.Components), exceptionMessage);
            tableWidgetConfiguration.DateRange = GetOptionalValueFromRowOrNull<string>(row, nameof(tableWidgetConfiguration.DateRange), exceptionMessage);

        }
        catch (Exception exception)
        {
            tableWidgetConfiguration.Exception = exception.Message;
        }

        return tableWidgetConfiguration;
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