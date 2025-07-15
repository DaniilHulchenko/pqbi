using System.Security.Cryptography.X509Certificates;

namespace PQBI.Infrastructure.Extensions;

public static class PqbiStringExtensions
{
    public static bool IsGuidEmpty(this string str)
    {
        var isEmpty = true;
        if (string.IsNullOrEmpty(str) == false)
        {
            if (Guid.TryParse(str, out var guid))
            {
                isEmpty = Guid.Empty == guid;
            }
        }

        return isEmpty;
    }

    public static bool IsStringEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    public static bool IsStringExists(this string str)
    {
        return !IsStringEmpty(str);
    }

    public static bool IsGuidNotEmpty(this string str)
    {
        return !IsGuidEmpty(str);
    }

    public static string UrlCombine(this string url, params string[] additionalResources)
    {
        if (additionalResources == null)
        {
            return url ?? string.Empty;
        }

        var cleanUrl = string.Empty;

        if (!string.IsNullOrEmpty(url))
        {
            cleanUrl = url.TrimEnd('/');
        }


        var resources = string.Join("/", additionalResources.Select(x => x.TrimEnd('/')));


        return $"{cleanUrl}/{resources}";
    }

}
