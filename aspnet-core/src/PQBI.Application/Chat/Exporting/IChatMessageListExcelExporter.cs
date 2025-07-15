using System.Collections.Generic;
using Abp;
using PQBI.Chat.Dto;
using PQBI.Dto;

namespace PQBI.Chat.Exporting
{
    public interface IChatMessageListExcelExporter
    {
        FileDto ExportToFile(UserIdentifier user, List<ChatMessageExportDto> messages);
    }
}
