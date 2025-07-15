namespace PQSServiceMock.Model.Dtos
{
    public class SetSessionStatusRequest
    {
        public string StateAlias { get; set; }
    }

    public class SetSessionStateResponse
    {
        public bool SessionStatus { get; set; }
    }
}
