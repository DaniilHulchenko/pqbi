namespace PQBI.Infrastructure.Extensions;

public static class PQBIEnumerableExtensions
{

    public static bool IsCollectionEmpty<TClass>(this IEnumerable<TClass> list)
    {
        if (list == null || list.Any() == false)
        {
            return true;
        }

        return false;
    }

    public static bool IsCollectionExists<TClass>(this IEnumerable<TClass> list) 
    {
        return !IsCollectionEmpty(list);
    }

    public static bool SafeAny<TClass>(this IEnumerable<TClass> list)
    {
        if (list == null || list.Any() == false)
        {
            return false;
        }

        return true;
    }
    

    public static IEnumerable<TClass> TakeSafe<TClass>(this IEnumerable<TClass> source, int take)
    {
        var list = new List<TClass>();

        if (source.IsCollectionEmpty() != true)
        {
            var amount = Math.Min(take, source.Count());
            for (int i = 0; i < amount; i++)
            {
                list.Add(source.ElementAt(i));
            }
        }

        return list;
    }



    public static List<TArget> SafeList<TArget>(this IEnumerable<TArget> collection)
    {
        var list = new List<TArget>();
        if (collection == null)
        {
            return list;
        }

        foreach (var item2 in collection)
        {
            if (item2 is TArget item)
            {
                list.Add(item);
            }
        }

        return list;
    }

    public static TArget[] SafeArray<TArget>(this IEnumerable<TArget> collection)
    {
        if (collection == null)
        {
            return [];
        }

        var list = new List<TArget>();
        foreach (var item2 in collection)
        {
            if (item2 is TArget item)
            {
                list.Add(item);
            }
        }

        return list.ToArray();
    }
}