using PQBI.Requests;
using PQS.Data.Permissions.Enums;

namespace PQBI.Network.Base
{
	public interface IPQSServiceBase
	{
        static string Alias = "PQSService";
    }


    public interface IPQSRestApiServiceBase: IPQSServiceBase
    {
        Task<PQSGetSessionResponse> OpenSessionForUserAsync(string url, string userName, string password);
        Task<bool> SendNOPForUserAsync(string url, string session);
        Task<bool> CloseSessionForUserAsync(string url, string session);
        Task<string> RequestXmlAsync(string url, string request);
        Task<string> IndentifyAsync(string url);
        Task<string> GetUserRole(string session, string url, string userName);


    }


    public abstract class PQSServiceBase
    {

        protected const string OPEN_SESSION_REQUEST = @"<Req>
	<ID>{0}</ID>
	<SessionID>00000000-0000-0000-0000-000000000000</SessionID>
	<Priority>MEDIUM</Priority>
	<Record>
		<Type>OPERATION_REQ</Type>
		<Data>
			<OperationType>OPEN_SESSION</OperationType>
			<Parameters>
				<NumOfParameters>3</NumOfParameters>
				<Item>
					<Name>STD_IS_DATA_ENCRYPTED</Name>
					<NumOfValues>1</NumOfValues>
					<Values>False</Values>
				</Item>
				<Item>
					<Name>STD_USER_NAME</Name>
					<NumOfValues>1</NumOfValues>
					<Values>{1}</Values>
				</Item>
				<Item>
					<Name>STD_PASSWORD</Name>
					<NumOfValues>1</NumOfValues>
					<Values>{2}</Values>
				</Item>
				<Item>
					<Name>STD_LOGGED_USER_SOURCE_TYPE</Name>
					<NumOfValues>1</NumOfValues>
					<Values>8</Values>				
				</Item>
			</Parameters>
		</Data>
	</Record>
</Req>
";

        protected const string NOP_SESSION_REQUEST = @"<Req>
    <ID>{0}</ID> 
	<SessionID>{1}</SessionID>
	<Priority>MEDIUM</Priority> 
	<Record> 
		<Type>OPERATION_REQ</Type> 
		<Data> 
			<OperationType>NOP</OperationType> 
			<Parameters> 
				<NumOfParameters>0</NumOfParameters> 
			</Parameters> 
		</Data> 
	</Record> 
</Req>";

        protected const string Close_Session_Request = @"<Req>
	<ID>{0}</ID>
	<SessionID>{1}</SessionID>
	<Priority>MEDIUM</Priority>
	<Record>
		<Type>OPERATION_REQ</Type>
		<Data>
			<OperationType>CLOSE_SESSION</OperationType>
			<Parameters>
				<NumOfParameters>1</NumOfParameters>
				<Item>
					<Name>STD_SESSION_ID</Name>
					<NumOfValues>1</NumOfValues>
					<Values>{2}</Values>
				</Item>
			</Parameters>
		</Data>
	</Record>
</Req>";


        /// <summary>
        /// Since request ID is not managed - We will populate it with fake data 40 time 0.
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="times"></param>
        /// <returns></returns>
        protected string Repeat(char sign = '0', int times = 32) => new string(sign, times);
    }
}
