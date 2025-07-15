namespace PQBI.Infrastructure.Lockers;

public class PqbiSafeEntityLockerSlim<T>
{
    protected T _item;
    protected static SemaphoreSlim _locker;


    public PqbiSafeEntityLockerSlim(T item)
    {
        if (item == null)
        {
            throw new NullReferenceException("Null is not an option");
        }

        _item = item;
        _locker = new SemaphoreSlim(1, 1);

    }

    public virtual T Value
    {
        get
        {

            _locker.Wait();
            try
            {
                return _item;
            }
            finally
            {
                _locker.Release();
            }
        }
        set
        {
            _locker.Wait();
            try
            {
                _item = value;
            }
            finally
            {
                _locker.Release();
            }
        }
    }

    public void Do(Action<T> safeAction)
    {
        _locker.Wait();

        try
        {
            safeAction(_item);
        }
        finally
        {
            _locker.Release();
        }
    }

    public async Task DoLockAsync(Action<T> asyncAction)
    {
        await _locker.WaitAsync();

        try
        {
            await Task.Run(() => asyncAction(_item));
        }
        finally
        {
            _locker.Release();
        }
    }

    public async Task<TResponse> DoFuncAsync<TResponse>(Func<T, Task<TResponse>> safeAction)
    {
        await _locker.WaitAsync();
        try
        {
            var result = await safeAction(_item);
            return result;
        }
        finally { _locker.Release(); }
    }
}