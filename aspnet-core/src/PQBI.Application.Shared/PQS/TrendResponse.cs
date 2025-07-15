using PQBI.PQS.CalcEngine;
using PQS.Data.Common.Values;
using System.Collections.Generic;

namespace PQBI.PQS;

public class TrendResponse
{
    public List<CalculatedDataItem> Data { get; set; } = new List<CalculatedDataItem>();

    public IEnumerable<long> TimeStamps { get; set; }

    public bool IsSuccess { get; set; } = true;

    public int Reason { get; set; }
}

public class CalculatedDataItem
{
    public string ParameterName { get; set; }

    public string ParameterType { get; set; }

    public IEnumerable<FeederComponentInfo> Feeders { get; set; }

    public string Response { get; set; }

    public List<double?> Data { get; set; } = new List<double?>();

    public double Nominal { get; set; }

    public List<DataValueStatus> Status { get; set; } = new List<DataValueStatus>();

    public List<MissingBaseParameterInfo> MissingInformation { get; set; } = new List<MissingBaseParameterInfo>();
}
