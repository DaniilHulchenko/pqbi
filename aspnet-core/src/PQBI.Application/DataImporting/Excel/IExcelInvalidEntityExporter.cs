using System.Collections.Generic;
using Abp.Dependency;
using PQBI.Dto;

namespace PQBI.DataImporting.Excel;

public interface IExcelInvalidEntityExporter<TEntityDto> : ITransientDependency
{
    FileDto ExportToFile(List<TEntityDto> entities);
}