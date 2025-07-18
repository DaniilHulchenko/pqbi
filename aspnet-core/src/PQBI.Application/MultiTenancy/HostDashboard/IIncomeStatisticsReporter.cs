﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PQBI.MultiTenancy.HostDashboard.Dto;

namespace PQBI.MultiTenancy.HostDashboard
{
    public interface IIncomeStatisticsService
    {
        Task<List<IncomeStastistic>> GetIncomeStatisticsData(DateTime startDate, DateTime endDate,
            ChartDateInterval dateInterval);
    }
}