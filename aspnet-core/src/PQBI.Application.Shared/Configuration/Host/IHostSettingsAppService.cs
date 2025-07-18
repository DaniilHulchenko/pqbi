﻿using System.Threading.Tasks;
using Abp.Application.Services;
using PQBI.Configuration.Host.Dto;

namespace PQBI.Configuration.Host
{
    public interface IHostSettingsAppService : IApplicationService
    {
        Task<HostSettingsEditDto> GetAllSettings();

        Task UpdateAllSettings(HostSettingsEditDto input);

        Task SendTestEmail(SendTestEmailInput input);
    }
}
