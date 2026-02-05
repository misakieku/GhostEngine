using Misaki.HighPerformance.Buffer;

namespace Ghost.Core.Utilities;

public class CollectionPool<TCollection, TItem>
    where TCollection : class, ICollection<TItem>, new()
{
    internal static readonly ObjectPool<TCollection> s_pool = new ObjectPool<TCollection>(() => new TCollection(), null, 1);

    public static TCollection Rent()
    {
        return s_pool.Rent();
    }

    public static void Return(TCollection collection)
    {
        collection.Clear();
        s_pool.Return(collection);
    }
}

public class ListPool<T> : CollectionPool<List<T>, T>;
public class HashSetPool<T> : CollectionPool<HashSet<T>, T>;
public class DictionaryPool<TKey, TValue> : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
    where TKey : notnull;