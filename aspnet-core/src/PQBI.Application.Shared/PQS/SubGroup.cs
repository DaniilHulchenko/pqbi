using System;

namespace PQBI.PQS
{
    public class SubGroupWithNull
    {
        public string Name { get; set; }
        public int? FromVal { get; set; }
        public int? ToVal { get; set; }
        public Guid Id { get; set; }
    }

    public class SubGroup
    {
        public string Name { get; set; }
        public int FromVal { get; set; }
        public int ToVal { get; set; }
        public Guid Id { get; set; }

        public override string ToString()
        {
            if (FromVal == int.MinValue && ToVal == int.MaxValue)
                return $"{Name}";
            if (FromVal == int.MinValue)
                return $"{Name}- <= {ToVal}";
            if (ToVal == int.MaxValue)
                return $"{Name}-{FromVal} <=";
            else
                return $"{Name}-{FromVal}:{ToVal}";
        }
    }
}
