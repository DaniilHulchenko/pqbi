using System;

namespace PQBI.PQS;

public record GetEventstRequest(DateTime Start, DateTime End, params string[] ComponentIds);

