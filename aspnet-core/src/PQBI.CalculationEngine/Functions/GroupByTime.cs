namespace PQBI.CalculationEngine.Functions
{
    public class GroupByTime
    {
        public static IReadOnlyList<IEnumerable<BasicValue>> AggregateToBars(
           IEnumerable<BasicValue> samples,          // raw signal, any cadence
           IEnumerable<(DateTime Start, DateTime End)> buckets)      
        {
            var ordered = samples.OrderBy(s => s.StartTime).ToList();
            int i = 0;                         // cursor in samples
            BasicValue? carry = null;          // “last known” value for forward‑fill
            var bars = new List<List<BasicValue>>();

            foreach (var (start, end) in buckets)
            {
                var slice = new List<BasicValue>();

                // collect all samples whose timestamp ∈ [start, end)
                while (i < ordered.Count && ordered[i].StartTime < end)
                {
                    if (ordered[i].StartTime >= start)
                        slice.Add(ordered[i]);

                    carry = ordered[i];       // update carry
                    i++;
                }

                // no fresh data in this bucket?  use the carry if allowed
                if (slice.Count == 0 && carry is not null)
                    slice.Add(carry.Value);
                //slice.Add(carry.Value with { StartTime = start });

                bars.Add(slice);
                //bars.Add(AggregateSlice(slice, kind, start));
            }
            return bars;
        }
    }
}
