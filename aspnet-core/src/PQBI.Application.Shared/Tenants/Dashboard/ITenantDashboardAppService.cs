using Abp.Application.Services;
using PQBI.PQS;
using PQBI.PQS.CalcEngine;
using PQBI.Tenants.Dashboard.Dto;
using System.Threading.Tasks;

namespace PQBI.Tenants.Dashboard
{
    public interface ITenantDashboardAppService : IApplicationService
    {
        GetMemberActivityOutput GetMemberActivity();

        GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input);

        GetDailySalesOutput GetDailySales();

        GetProfitShareOutput GetProfitShare();

        GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input);

        GetTopStatsOutput GetTopStats();

        GetRegionalStatsOutput GetRegionalStats();

        GetGeneralStatsOutput GetGeneralStats();

        Task<TableWidgetResponse> PQSTableWidgetData2222Async(TableWidgetRequest222 request);
        Task<BarChartResponse> PQSBarChartWidgetData( BarChartRequest request);


        Task<TrendResponse> PQSTrendData222(TrendCalcRequest222 request);

    }
}
