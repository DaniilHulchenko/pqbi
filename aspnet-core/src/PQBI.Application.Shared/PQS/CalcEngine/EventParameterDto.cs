namespace PQBI.PQS.CalcEngine
{
    public class EventParameterDto
    {
        public TableWidgetEvent TableEvent { get; set; }

        public NormalizeEnum Normalize { get; set; }

        public double? NormalValue { get; set; }

        public string ParameterName { get; set; } = string.Empty;
        public string ReplaceAggregationWith { get; set; }
    }
}
