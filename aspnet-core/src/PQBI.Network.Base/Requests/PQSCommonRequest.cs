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
    public PQSCommonRequest(string session) : base()
    {
        SessionID = Guid.Parse(session);
    }

    public string Session => SessionID.ToString();
}


public abstract class PQSCommonResponse<TRequest> : PQSOperationResponseBase<TRequest> where TRequest : PQSRequestBase
{
    public PQSCommonResponse(TRequest request, PQSResponse response) : base(request, response)
    {
    }
}
