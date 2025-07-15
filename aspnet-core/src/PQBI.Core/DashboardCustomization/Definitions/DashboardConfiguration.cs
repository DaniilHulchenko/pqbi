using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Abp.Authorization;
using Abp.Dependency;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using PayPalCheckoutSdk.Orders;
using PQBI.Authorization;
using PQBI.DashboardCustomization.Definitions.Cache;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PQBI.DashboardCustomization.Definitions
{
    public class DashboardConfiguration : ITransientDependency
    {
        public string TenantWidgetDefinitionsCacheName = "TenantWidgetDefinitionsCache";
        public const string HostWidgetDefinitionsCacheName = "HostWidgetDefinitionsCache";

        private readonly IDashboardDefinitionCacheManager _dashboardDefinitionCacheManager;
        private readonly IWidgetDefinitionCacheManager _widgetDefinitionCacheManager;
        private readonly IWidgetFilterDefinitionCacheManager _widgetFilterDefinitionCacheManager;

        private readonly IAbpSession _abpSession;

        private List<DashboardDefinition> DashboardDefinitions { get; } = new();
        private List<WidgetDefinition> WidgetDefinitions { get; } = new();
        private List<WidgetFilterDefinition> WidgetFilterDefinitions { get; } = new();

        public DashboardConfiguration(
            IDashboardDefinitionCacheManager dashboardDefinitionCacheManager,
            IWidgetDefinitionCacheManager widgetDefinitionCacheManager,
            IWidgetFilterDefinitionCacheManager widgetFilterDefinitionCacheManager,
            IAbpSession abpSession)
        {
            _dashboardDefinitionCacheManager = dashboardDefinitionCacheManager;
            _widgetDefinitionCacheManager = widgetDefinitionCacheManager;
            _widgetFilterDefinitionCacheManager = widgetFilterDefinitionCacheManager;
            _abpSession = abpSession;

            #region FilterDefinitions

            // These are global filter which all widgets can use
            var dateRangeFilter = new WidgetFilterDefinition(
                PQBIDashboardCustomizationConsts.Filters.FilterDateRangePicker,
                "FilterDateRangePicker"
            );
            //var PQSFilter = new WidgetFilterDefinition(
            //    PQBIDashboardCustomizationConsts.Filters.FilterPQS,
            //    "FilterPQS"
            //);

            WidgetFilterDefinitions.AddRange(new List<WidgetFilterDefinition>()
            {
                dateRangeFilter,
                // Add your filters here
            });

            #endregion

            #region WidgetDefinitions

            // Define Widgets

            #region TenantWidgets

            var simplePermissionDependencyForTenantDashboard =
                new PQBISimplePermissionDependency(AppPermissions.Pages_Tenant_Dashboard);

            //var dailySales = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.DailySales,
            //    "WidgetDailySales",
            //    side: MultiTenancySides.Tenant,
            //    usedWidgetFilters: new List<string> { dateRangeFilter.Id },
            //    permissionDependency: simplePermissionDependencyForTenantDashboard
            //);

            //var generalStats = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.GeneralStats,
            //    "WidgetGeneralStats",
            //    side: MultiTenancySides.Tenant,
            //    permissionDependency: new PQBISimplePermissionDependency(
            //        requiresAll: true,
            //        AppPermissions.Pages_Tenant_Dashboard,
            //        AppPermissions.Pages_Administration_AuditLogs
            //    )
            //);

            //var profitShare = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.ProfitShare,
            //    "WidgetProfitShare",
            //    side: MultiTenancySides.Tenant,
            //    permissionDependency: simplePermissionDependencyForTenantDashboard
            //);

            //var memberActivity = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.MemberActivity,
            //    "WidgetMemberActivity",
            //    side: MultiTenancySides.Tenant,
            //    permissionDependency: simplePermissionDependencyForTenantDashboard
            //);

            //var regionalStats = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.RegionalStats,
            //    "WidgetRegionalStats",
            //    side: MultiTenancySides.Tenant,
            //    permissionDependency: simplePermissionDependencyForTenantDashboard
            //);

            //var salesSummary = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.SalesSummary,
            //    "WidgetSalesSummary",
            //    usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
            //    side: MultiTenancySides.Tenant,
            //    permissionDependency: simplePermissionDependencyForTenantDashboard
            //);

            //var topStats = new WidgetDefinition(
            //    PQBIDashboardCustomizationConsts.Widgets.Tenant.TopStats,
            //    "WidgetTopStats",
            //    side: MultiTenancySides.Tenant,
            //    permissionDependency: simplePermissionDependencyForTenantDashboard
            //);

            var pqs = new WidgetDefinition(
                id: PQBIDashboardCustomizationConsts.Widgets.Tenant.PQSTrend,
                name: "WidgetPQSTrend",//localized string key
                side: MultiTenancySides.Tenant,
                //usedWidgetFilters: new List<string>() { dateRangeFilter.Id }, // new List<string>() { helloWorldFilter.Id },// you can use any filter you need
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var pqsTable = new WidgetDefinition(
                id: PQBIDashboardCustomizationConsts.Widgets.Tenant.PQSTable,
                name: "WidgetPQSTable",//localized string key
                side: MultiTenancySides.Tenant,
                usedWidgetFilters: new List<string>() { dateRangeFilter.Id }, 
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var pqsBarChart = new WidgetDefinition(
                id: PQBIDashboardCustomizationConsts.Widgets.Tenant.PQSBarChart,
                name: "WidgetPQSBarChart",//localized string key
                side: MultiTenancySides.Tenant,
                usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );


            WidgetDefinitions.AddRange(
                new List<WidgetDefinition>
                {
                    //generalStats,
                    //dailySales,
                    //profitShare,
                    //memberActivity,
                    //regionalStats,
                    //topStats,
                    //salesSummary,
                    pqs,
                    pqsTable,
                    pqsBarChart
                    // Add your tenant side widgets here
                });

            #endregion

            #region HostWidgets

            var simplePermissionDependencyForHostDashboard =
                new PQBISimplePermissionDependency(AppPermissions.Pages_Administration_Host_Dashboard);

            var incomeStatistics = new WidgetDefinition(
                PQBIDashboardCustomizationConsts.Widgets.Host.IncomeStatistics,
                "WidgetIncomeStatistics",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var hostTopStats = new WidgetDefinition(
                PQBIDashboardCustomizationConsts.Widgets.Host.TopStats,
                "WidgetTopStats",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var editionStatistics = new WidgetDefinition(
                PQBIDashboardCustomizationConsts.Widgets.Host.EditionStatistics,
                "WidgetEditionStatistics",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var subscriptionExpiringTenants = new WidgetDefinition(
                PQBIDashboardCustomizationConsts.Widgets.Host.SubscriptionExpiringTenants,
                "WidgetSubscriptionExpiringTenants",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var recentTenants = new WidgetDefinition(
                PQBIDashboardCustomizationConsts.Widgets.Host.RecentTenants,
                "WidgetRecentTenants",
                side: MultiTenancySides.Host,
                usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            WidgetDefinitions.AddRange(new List<WidgetDefinition>()
            {
                incomeStatistics,
                hostTopStats,
                editionStatistics,
                subscriptionExpiringTenants,
                recentTenants
                // Add your host side widgets here
            });

            #endregion

            #endregion

            #region DashboardDefinitions

            // Create dashboard
            var defaultTenantDashboard = new DashboardDefinition(
                PQBIDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
                new List<string>
                {
                    //generalStats.Id, dailySales.Id, profitShare.Id, memberActivity.Id, regionalStats.Id, topStats.Id,
                    //salesSummary.Id,
                    pqs.Id, pqsTable.Id, pqsBarChart.Id,
                });

            DashboardDefinitions.Add(defaultTenantDashboard);

            var defaultHostDashboard = new DashboardDefinition(
                PQBIDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
                new List<string>
                {
                    incomeStatistics.Id,
                    hostTopStats.Id,
                    editionStatistics.Id,
                    subscriptionExpiringTenants.Id,
                    recentTenants.Id
                });

            DashboardDefinitions.Add(defaultHostDashboard);

            // Add your dashboard definition here

            #endregion
        }

        public DashboardDefinition GetDashboardDefinition(string name)
        {
            var dashboardDefinition = _dashboardDefinitionCacheManager.Get(name);
            if (dashboardDefinition == null)
            {
                dashboardDefinition = DashboardDefinitions.Find(d => d.Name == name);
                _dashboardDefinitionCacheManager.Set(dashboardDefinition);
            }

            return dashboardDefinition;
        }

        public WidgetDefinition GetWidgetDefinition(string id)
        {
            var widgets = GetWidgetDefinitions();
            return widgets.Find(w => w.Id == id);
        }

        public List<WidgetDefinition> GetWidgetDefinitions()
        {
            var widgetDefinitionKey = _abpSession.MultiTenancySide == MultiTenancySides.Host
                ? HostWidgetDefinitionsCacheName
                : TenantWidgetDefinitionsCacheName;

            var widgetDefinitions = _widgetDefinitionCacheManager.GetAll(widgetDefinitionKey);
            if (widgetDefinitions == null)
            {
                widgetDefinitions = WidgetDefinitions.Where(e => e.Side == _abpSession.MultiTenancySide).ToList();
                _widgetDefinitionCacheManager.Set(widgetDefinitionKey, widgetDefinitions);
            }

            return widgetDefinitions;
        }

        public List<WidgetFilterDefinition> GetWidgetFilterDefinitions()
        {
            var filterDefinitions = _widgetFilterDefinitionCacheManager.GetAll();
            if (filterDefinitions == null)
            {
                filterDefinitions = WidgetFilterDefinitions;
                _widgetFilterDefinitionCacheManager.Set(filterDefinitions);
            }

            return filterDefinitions;
        }
    }
}