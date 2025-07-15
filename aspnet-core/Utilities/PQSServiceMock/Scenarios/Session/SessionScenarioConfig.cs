using Microsoft.Extensions.Options;
using PQBI.Infrastructure;

namespace PQSServiceMock.Scenarios.Session; 


public class SessionScenarioConfig : PQSConfig<SessionScenarioConfig>
{
    //takeValid__10__failed__1 ==> skip__10__fail
    //takeValid__0__failed___1  ==> skip__0__fail
    public string NopServiceRecipe { get; set; }
}


public interface ISessionScenario
{
    bool Authenticate(string xmlRequest);
    bool NopService(string xmlRequest);
}

public enum SessionCommandType
{
    TakeValid,
    Failed
}

public static class SessionScenarioConfigExtensions
{
    public static int TransformScnarioNop(this SessionScenarioConfig config)
    {
        var nops = config.NopServiceRecipe.Split("__");
        if (nops[0].Equals(nameof(SessionCommandType.TakeValid), StringComparison.OrdinalIgnoreCase))
        {
            return int.Parse(nops[1]);
        }

        throw new NotImplementedException();
    }

    public static void OnValidate(this SessionScenarioConfig config)
    {
        var nopCmd = config.NopServiceRecipe;
        var tokens = nopCmd.Split("__");

        if (!Enum.TryParse<SessionCommandType>(tokens[0], out _))
        {
            throw new ArgumentException("First Command");
        }

        if (!Enum.TryParse<SessionCommandType>(tokens[2], out _))
        {
            throw new ArgumentException("Last Command");
        }

        int.Parse(tokens[1]);
        int.Parse(tokens[3]);
    }
}

public class SessionScenario : ISessionScenario
{
    private int _validScenarioNop;

    public SessionScenario(IOptions<SessionScenarioConfig> option)
    {
        var config = option.Value;
        config.OnValidate();

        _validScenarioNop = config.TransformScnarioNop();
    }

    public bool Authenticate(string xmlRequest)
    {
        return true;
    }

    public bool NopService(string xmlRequest)
    {
        if (_validScenarioNop-- > 0)
        {
            return true;
        }

        return false;
    }
}

