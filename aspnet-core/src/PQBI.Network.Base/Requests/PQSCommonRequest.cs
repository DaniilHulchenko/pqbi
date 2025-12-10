using PQS.Data.RecordsContainer;

namespace PQBI.Requests;

public abstract class PQSRequestBase : PQSRequest
{
    public PQSRequestBase()
        : base()
    {
    }

    protected abstract void AddConfigurations();

}

public abstract class PQSCommonRequest : PQSRequestBase
{
    public uint? TimeZoneID { get; set; }

    public PQSCommonRequest(string session, uint? timeZoneID = null) : base()
    {
        SessionID = Guid.Parse(session);
        TimeZoneID = timeZoneID;
    }

    public string Session => SessionID.ToString();
}


public class PQSCommonResponse<TRequest> : PQSOperationResponseBase<TRequest> where TRequest : PQSRequest
{
    public PQSCommonResponse(TRequest request, PQSResponse response, string timezone) : base(request, response, timezone)
    {
    }
}
