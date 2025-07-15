
using System.Collections.Generic;

namespace PQBI.PQS;

public record PQSEventDto(int EventClass, double Value, double Deviation, System.DateTime StartTime, int DurationMilliSecond, List<string> Phases, List<uint>Feeders);