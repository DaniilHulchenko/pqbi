using System;
using System.Collections.Generic;
using System.Text;

namespace PQBI.Tenants.Dashboard.Dto
{
    public class CustomParameter
    {
        public string Name { get; set; }
        public CPValue Value { get; set; }
    }

    public class CPValue
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AggregationFunction { get; set; }
        public PQBIParameterInfo[] ParameterList { get; set; }
    }

    public class PQBIParameterInfo
    {
        public string ParamName { get; set; }
        public string ComponentId { get; set; }
        public string Quantity { get; set; }
    }

    public class Function 
    {
        public string Name { get; set; }
        public string? Arg { get; set; }
    }

    public class PQSInput
    {
        public Function[] Functions { get; set; }
        public object[] ApplyTo { get; set; }
        public CustomParameter CustomParameter { get; set; }
        public object[] Feeders { get; set; }
        public string Resolution { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }


}
