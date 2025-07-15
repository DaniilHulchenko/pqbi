using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI
{
    public enum PQBIQuantityType
    {
        min,
        max,
        avg,
        percentile,   // use `percentileRank` argument
        count
    }
}
