using Abp.UI;
using PQS.Data.Common;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;

namespace PQBI;

public class PropertyServiceError
{
    public PropertyServiceError(string title, string errorMessage)
    {
        PropertyName = title;
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; }
    public string PropertyName { get; }

}


//TODO need to be fully refactored!!!!!
public class PQBIException : UserFriendlyException
{
    public PQBIException(PropertyServiceError globalError)
        : base(globalError.PropertyName, globalError.ErrorMessage)
    {
    }
}

public class SessionEmptyException : PQBIExceptionBase

{
    public SessionEmptyException(PQSResponse response = null)
        : base("Session", response, "Please logout and then log in.")
    {
        Code = SubCode = (int)PQBISubCodeType.SessionScadaExpreration;
    }
}

public enum PQBISubCodeType : int
{
    SessionScadaExpreration = 1
}

public class PQBIExceptionBase : UserFriendlyException
{

    public PQBIExceptionBase(string message, PQSResponse response, string? details = null)
                : base(message, details)

    {
        PQSResponse = response;
    }



    public int SubCode { get; protected set; }
    public ErrorRecord ErrorRecord
    {
        get
        {

            var record = PQSResponse.GetRecords().First();
            if (record is ErrorRecord error)
            {
                return error;
            }

            return null;
        }
    }

    public PQSResponse PQSResponse { get; set; }
}

public class SessionExpiredException : PQBIExceptionBase
{
    public SessionExpiredException(PQSResponse response )
        : base("Session expired: Connection with SCADA was lost, hence please logout and log in.", response)
    {
        Code = SubCode = (int)PQBISubCodeType.SessionScadaExpreration;
    }
}

//public class NoDataForGroupException : UserFriendlyException
//{
//    public NoDataForGroupException(PQZStatus status, string group)
//        : base("No Data", $"[{status.ToString()}] since for group [{group}] no data.")
//    {
//    }
//}


public class TrendArgumentException : UserFriendlyException
{
    public TrendArgumentException(string message)
        : base(message)
    {
    }

}

public class ArithmeticNotSupportedException : UserFriendlyException
{
    public ArithmeticNotSupportedException()
        : base("Arythmetic not supported")
    {
    }

}