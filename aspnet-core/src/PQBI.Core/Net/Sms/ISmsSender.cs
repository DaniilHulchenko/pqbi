using System.Threading.Tasks;

namespace PQBI.Net.Sms
{
    public interface ISmsSender
    {
        Task SendAsync(string number, string message);
    }
}