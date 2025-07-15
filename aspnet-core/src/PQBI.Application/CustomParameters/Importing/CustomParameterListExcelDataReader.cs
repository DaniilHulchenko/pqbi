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
using PQBI.CustomParameters.Importing.Dto;

namespace PQBI.CustomParameters;

public class CustomParameterListExcelDataReader(ILocalizationManager localizationManager)
    : MiniExcelExcelImporterBase<ImportCustomParameterDto>, IExcelDataReader<ImportCustomParameterDto>
{
    private readonly ILocalizationSource _localizationSource = localizationManager.GetSource(PQBIConsts.LocalizationSourceName);

public List<ImportCustomParameterDto> GetEntitiesFromExcel(byte[] fileBytes)
{
    return ProcessExcelFile(fileBytes, ProcessExcelRow);
}

private ImportCustomParameterDto ProcessExcelRow(dynamic row)
{

    var exceptionMessage = new StringBuilder();
    var customParameter = new ImportCustomParameterDto();

    try
    {
        customParameter.Name = GetRequiredValueFromRowOrNull(row, nameof(customParameter.Name), exceptionMessage);
        customParameter.AggregationFunction = GetRequiredValueFromRowOrNull(row, nameof(customParameter.AggregationFunction), exceptionMessage);
        customParameter.Type = GetRequiredValueFromRowOrNull(row, nameof(customParameter.Type), exceptionMessage);
        customParameter.InnerCustomParameters = GetOptionalValueFromRowOrNull<string>(row, nameof(customParameter.InnerCustomParameters), exceptionMessage);
        customParameter.ResolutionInSeconds = Convert.ToInt32(GetOptionalValueFromRowOrNull<int>(row, nameof(customParameter.ResolutionInSeconds), exceptionMessage));
        customParameter.CustomBaseDataList = GetOptionalValueFromRowOrNull<string>(row, nameof(customParameter.CustomBaseDataList), exceptionMessage);

    }
    catch (Exception exception)
    {
        customParameter.Exception = exception.Message;
    }

    return customParameter;
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