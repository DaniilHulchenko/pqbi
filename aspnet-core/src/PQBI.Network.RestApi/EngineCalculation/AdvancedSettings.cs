using PQBI.PQS.CalcEngine;
using PQS.Data.Events;
using PQS.Data.Events.Enums;
using PQS.Data.Events.Filters;

namespace PQBI.Network.RestApi.EngineCalculation
{
    public class AdvancedSettings
    {
        public NormalizeEnum NormalizeType { get; }
        public double? NominalVal { get; }
        public FiltersGroup FiltersGroup { get; }
        public bool IsExcludeFlaggedData { get; set; }
        public bool IsIgnoreAligningFunction { get; set; }
        public string? ReplaceOuterAggregationWith { get; set; }

        public AdvancedSettings(double? nominalVal, NormalizeEnum normalizeType, bool isExcludeFlaggedData, IEnumerable<EventClass> excludeFlaggedCollection, bool isIgnoreAligningFunction, string? replaceOuterAggregationWith)
        {
            NominalVal = nominalVal;
            NormalizeType = normalizeType;

            if (isExcludeFlaggedData || (excludeFlaggedCollection != null && excludeFlaggedCollection.Count() > 0))
                IsExcludeFlaggedData = true;
            FiltersGroup filtersGroup = GetFilterGroup(excludeFlaggedCollection);

            FiltersGroup = filtersGroup;
            IsIgnoreAligningFunction = isIgnoreAligningFunction;
            ReplaceOuterAggregationWith = replaceOuterAggregationWith;
        }

        public static FiltersGroup GetFilterGroup(IEnumerable<EventClass> excludeFlaggedCollection)
        {
            FiltersGroup filtersGroup = new FiltersGroup();

            if (excludeFlaggedCollection != null && excludeFlaggedCollection.Count() > 0)
            {
                ClassFilter classFilter = new ClassFilter();
                foreach (EventClass item in excludeFlaggedCollection)
                {
                    classFilter.AddSingleValue(item);
                }
                filtersGroup.AddFilter(classFilter);
            }

            return filtersGroup;
        }
    }

    //public enum NormalizeEnum
    //{
    //    NO,
    //    NOMINAL,
    //    VALUE
    //}
}
