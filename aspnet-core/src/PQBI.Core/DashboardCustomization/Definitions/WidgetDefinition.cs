using System.Collections.Generic;
using Abp.Authorization;
using Abp.Application.Features;
using Abp.MultiTenancy;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace PQBI.DashboardCustomization.Definitions
{
    [JsonConverter(typeof(WidgetDefinitionConverter))]
    public class WidgetDefinition
    {

        public string Id { get; }

        public string Name { get; }

        public MultiTenancySides Side { get; }

        public IPermissionDependency PermissionDependency { get; }

        public List<string> UsedWidgetFilters { get; }

        public string Description { get; }

        public bool AllowMultipleInstanceInSamePage { get; }

        public IFeatureDependency FeatureDependency { get; }

        public string WidgetGuid { get; }

        public WidgetDefinition(
            string id,
            string name,
            MultiTenancySides side = MultiTenancySides.Tenant | MultiTenancySides.Host,
            List<string> usedWidgetFilters = null,
            IPermissionDependency permissionDependency = null,
            string description = null,
            bool allowMultipleInstanceInSamePage = true,
            IFeatureDependency featureDependency = null,
            string widgetguid = null)
        {
            Id = id;
            Name = name;
            Side = side;
            UsedWidgetFilters = usedWidgetFilters;
            PermissionDependency = permissionDependency;
            Description = description;
            AllowMultipleInstanceInSamePage = allowMultipleInstanceInSamePage;
            FeatureDependency = featureDependency;
            WidgetGuid = widgetguid;
        }
    }
}