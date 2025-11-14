using Abp.Auditing;
using Abp.Runtime.Session;
using PQBI.Caching;
using PQBI.Network.RestApi;
using PQBI.PQS.CalcEngine;
using PQBI.PQS;
using PQBI.Tenants.Dashboard.Dto;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using PQBI.Groups;
using Newtonsoft.Json;

namespace PQBI.Tenants.Dashboard
{
    [DisableAuditing]
    //[AbpAuthorize(AppPermissions.Pages_Tenant_Dashboard)]
    public class TenantDashboardAppService : PQBIAppServiceBase, ITenantDashboardAppService
    {
        public const string PostCalculationUrl = $"/PQSTrendData";
        private readonly IRepository<Group, Guid> _groupRepository;
        private readonly ITenantCacheRepository _tenantCacheRepository;
        private readonly IUserSessionCacheRepository _userSessionCacheRepository;
        private readonly ICustomParameterCalculationService _customParameterCalculationService;


        public TenantDashboardAppService(ITenantCacheRepository tenantCacheRepository,
         IUserSessionCacheRepository userSessionCacheRepository,
         IRepository<Group, Guid> groupRepository,
         ICustomParameterCalculationService customParameterCalculationService
            )
        {
            _tenantCacheRepository = tenantCacheRepository;
            _userSessionCacheRepository = userSessionCacheRepository;
            _groupRepository = groupRepository;
            _customParameterCalculationService = customParameterCalculationService;
        }
        public GetMemberActivityOutput GetMemberActivity()
        {
            return new GetMemberActivityOutput
            (
                DashboardRandomDataGenerator.GenerateMemberActivities()
            );
        }


        public GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input)
        {
            var output = new GetDashboardDataOutput
            {
                TotalProfit = DashboardRandomDataGenerator.GetRandomInt(5000, 9000),
                NewFeedbacks = DashboardRandomDataGenerator.GetRandomInt(1000, 5000),
                NewOrders = DashboardRandomDataGenerator.GetRandomInt(100, 900),
                NewUsers = DashboardRandomDataGenerator.GetRandomInt(50, 500),
                SalesSummary = DashboardRandomDataGenerator.GenerateSalesSummaryData(input.SalesSummaryDatePeriod),
                Expenses = DashboardRandomDataGenerator.GetRandomInt(5000, 10000),
                Growth = DashboardRandomDataGenerator.GetRandomInt(5000, 10000),
                Revenue = DashboardRandomDataGenerator.GetRandomInt(1000, 9000),
                TotalSales = DashboardRandomDataGenerator.GetRandomInt(10000, 90000),
                TransactionPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                NewVisitPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                BouncePercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                DailySales = DashboardRandomDataGenerator.GetRandomArray(30, 10, 50),
                ProfitShares = DashboardRandomDataGenerator.GetRandomPercentageArray(3)
            };

            return output;
        }

        public GetTopStatsOutput GetTopStats()
        {
            return new GetTopStatsOutput
            {
                TotalProfit = DashboardRandomDataGenerator.GetRandomInt(5000, 9000),
                NewFeedbacks = DashboardRandomDataGenerator.GetRandomInt(1000, 5000),
                NewOrders = DashboardRandomDataGenerator.GetRandomInt(100, 900),
                NewUsers = DashboardRandomDataGenerator.GetRandomInt(50, 500)
            };
        }

        public GetProfitShareOutput GetProfitShare()
        {
            return new GetProfitShareOutput
            {
                ProfitShares = DashboardRandomDataGenerator.GetRandomPercentageArray(3)
            };
        }


        public GetDailySalesOutput GetDailySales()
        {
            return new GetDailySalesOutput
            {
                DailySales = DashboardRandomDataGenerator.GetRandomArray(30, 10, 50)
            };
        }

        public GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input)
        {
            var salesSummary = DashboardRandomDataGenerator.GenerateSalesSummaryData(input.SalesSummaryDatePeriod);
            return new GetSalesSummaryOutput(salesSummary)
            {
                Expenses = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
                Growth = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
                Revenue = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
                TotalSales = DashboardRandomDataGenerator.GetRandomInt(0, 3000)
            };
        }

        public GetRegionalStatsOutput GetRegionalStats()
        {
            return new GetRegionalStatsOutput(
                DashboardRandomDataGenerator.GenerateRegionalStat()
            );
        }

        public GetGeneralStatsOutput GetGeneralStats()
        {
            return new GetGeneralStatsOutput
            {
                TransactionPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                NewVisitPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                BouncePercent = DashboardRandomDataGenerator.GetRandomInt(10, 100)
            };
        }

        //public async Task<PQSCalculationResponse> PQSTrendData222(TrendCalcRequest222 request)
        //{
        //    var response = default(CalculationDto);

        //    if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        //    {
        //        var tenantId = AbpSession.GetTenantId();
        //        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        //        var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
        //        if (session.IsNullOrEmpty())
        //        {
        //            throw new SessionEmptyException(null);
        //        }

        //        try
        //        {
        //            response = await _customParameterCalculationService.CalculateTrendChartAsync(tenant.PQSServiceUrl, session, request);
        //            var respones = await _customParameterCalculationService.CalculateTrendChartAsync(tenant.PQSServiceUrl, session, request);
        //        }
        //        catch (PQBIException chattoException)
        //        {
        //            //response = new CalculationDto([], false, chattoException.Message);
        //        }
        //        catch (Exception ex)
        //        {
        //            //response = new CalculationDto([], false, ex.Message);
        //        }
        //    }


        //    return new PQSCalculationResponse(response);

        //}

        public async Task<TrendResponse> PQSTrendData(TrendCalcRequest request)
        {
            var response = default(TrendResponse);


            if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
            {
                var tenantId = AbpSession.GetTenantId();
                var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

                var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
                if (session.IsNullOrEmpty())
                {
                    throw new SessionEmptyException(null);
                }

                try
                {
                    response = await _customParameterCalculationService.CalculateTrendChartAsync(tenant.PQSServiceUrl, session, request);
                }
                catch (PQBIException chattoException)
                {
                    //response = new CalculationDto([], false, chattoException.Message);
                }
                catch (Exception ex)
                {
                    //response = new CalculationDto([], false, ex.Message);
                }
            }


            return response;

        }


        //public async Task<TrendResponse> PQSTrendData333(TrendCalcRequest222 request)
        //{
        //    var response = default(TrendResponse);


        //    if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        //    {
        //        var tenantId = AbpSession.GetTenantId();
        //        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        //        var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
        //        if (session.IsNullOrEmpty())
        //        {
        //            throw new SessionEmptyException(null);
        //        }

        //        try
        //        {
        //            response = await _customParameterCalculationService.CalculateTrendChartAsync222(tenant.PQSServiceUrl, session, request);
        //        }
        //        catch (PQBIException chattoException)
        //        {
        //            //response = new CalculationDto([], false, chattoException.Message);
        //        }
        //        catch (Exception ex)
        //        {
        //            //response = new CalculationDto([], false, ex.Message);
        //        }
        //    }


        //    return response;

        //}

        //-------------------------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------------------------


        //[HttpPost]
        //[AbpAuthorize(AppPermissions.PQSPermission)]
        //public async Task<BarChartResponse> PQSBarChartWidgetData([FromBody] BarChartRequest request)
        public async Task<BarChartResponse> PQSBarChartWidgetData(BarChartRequest request)
        {
            BarChartResponse response = null;

            if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
            {
                var tenantId = AbpSession.GetTenantId();
                var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

                var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
                if (session.IsNullOrEmpty())
                {
                    throw new SessionEmptyException();
                }

                List<SubGroup> subgroups = null;
                //switch (request.Category.Type)
                //{
                //    case DimensionType.CustomGroup:
                //        {
                //            if (request.Category.Id.HasValue)
                //            {
                //                Group customGroup = await _groupRepository.GetAsync(request.Category.Id.Value);

                //                var raw = JsonConvert.DeserializeObject<List<SubGroupWithNull>>(customGroup.Subgroups);
                //                subgroups = raw.Select(g => new SubGroup
                //                {
                //                    Name = g.Name,
                //                    FromVal = g.FromVal ?? int.MinValue,
                //                    ToVal = g.ToVal ?? int.MaxValue,
                //                    Id = g.Id
                //                }).ToList();
                //            }
                //        }
                //        break;
                //}

                Guid? groupID = null;
                if (request.Category.Type == DimensionType.CustomGroup && request.Category.Id.HasValue)
                    groupID = request.Category.Id.Value;
                else if (request.SeriesBy.Type == DimensionType.CustomGroup && request.SeriesBy.Id.HasValue)
                    groupID = request.SeriesBy.Id.Value;

                if (groupID.HasValue)
                {
                    Group customGroup = await _groupRepository.GetAsync(groupID.Value);                   
                    var raw = JsonConvert.DeserializeObject<List<SubGroupWithNull>>(customGroup.Subgroups);
                    subgroups = raw.Select(g => new SubGroup
                    {
                        Name = g.Name,
                        FromVal = g.FromVal ?? int.MinValue,
                        ToVal = g.ToVal ?? int.MaxValue,
                        Id = g.Id
                    }).ToList();
                }

                response = await _customParameterCalculationService.CalculateBarChartAsync(tenant.PQSServiceUrl, session, request, subgroups);

                //response = new BarChartResponse
                //{
                //    Components = items.ToList(),
                //    StartDate = request.StartDate,
                //    EndDate = request.EndDate,
                //    //Config = request.Config
                //};

            }

            return response;
        }

        public async Task<TableWidgetResponse> PQSTableWidgetDataAsync(TableWidgetRequest request)
        {
            TableWidgetResponse response = null;

            if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
            {
                var tenantId = AbpSession.GetTenantId();
                var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

                var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
                if (session.IsNullOrEmpty())
                {
                    throw new SessionEmptyException();
                }

                response = await _customParameterCalculationService.CalculateTableAsync(tenant.PQSServiceUrl, session, request);
                //response.Config = request.Config;

            }

            return response;
        }

        public async Task<TableWidgetResponse> PQSCardWidgetDataAsync(TableWidgetRequest request)
        {
            TableWidgetResponse response = null;

            if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
            {
                var tenantId = AbpSession.GetTenantId();
                var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

                var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
                if (session.IsNullOrEmpty())
                {
                    throw new SessionEmptyException();
                }

                response = await _customParameterCalculationService.CalculateCardAsync(tenant.PQSServiceUrl, session, request);               

            }

            return response;
        }

        public async Task<TableWidgetResponse> PQSGaugeWidgetDataAsync(TableWidgetRequest request)
        {
            TableWidgetResponse response = null;

            if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
            {
                var tenantId = AbpSession.GetTenantId();
                var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

                var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
                if (session.IsNullOrEmpty())
                {
                    throw new SessionEmptyException();
                }

                response = await _customParameterCalculationService.CalculateGaugeAsync(tenant.PQSServiceUrl, session, request);

            }

            return response;
        }


    }
}