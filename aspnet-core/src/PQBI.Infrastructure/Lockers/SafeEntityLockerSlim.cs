namespace PQBI.Infrastructure.Lockers;

public class SafeEntityLockerSlim<T>
{
    protected T _item;
    protected ReaderWriterLockSlim _locker;

    public SafeEntityLockerSlim(T item)
    {
        if (item == null)
        {
            throw new NullReferenceException("Null is not an option");
        }

        _item = item;
        _locker = new ReaderWriterLockSlim();
    }

    public virtual T Value
    {
        get
        {
            _locker.EnterReadLock();
            try
            {
                return _item;
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }
        set
        {
            _locker.EnterWriteLock();
            try
            {
                _item = value;
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }

    public void Do(Action<T> safeAction)
    {
        _locker.EnterWriteLock();

        try
        {
            safeAction(_item);
        }
        finally
        {
            _locker.ExitWriteLock();
        }
    }

    public async Task<LockRelease> ReaderLockAsync()
    {
        await Task.Yield();
        _locker.EnterReadLock();
        return new LockRelease(_locker, LockerType.Read);
    }

    public async Task<LockRelease> WriterLockAsync()
    {
        await Task.Yield();
        _locker.EnterWriteLock();
        return new LockRelease(_locker, LockerType.Write);
    }

    public async Task<TResponse> DoFuncAsync<TResponse>(Func<T, Task<TResponse>> safeAction)
    {
        using (await WriterLockAsync())
        {
            var result = await safeAction(_item);
            return result;
        }
    }

    public async Task DoActionAsync(Func<T, Task> safeAction)
    {
        using (await WriterLockAsync())
        {
            await safeAction(_item);
        }
    }
}



public struct LockRelease : IDisposable
{
    private readonly ReaderWriterLockSlim _locker;
    private readonly LockerType _lockerType;

    public LockRelease(ReaderWriterLockSlim locker, LockerType lockerType)
    {
        _locker = locker;
        _lockerType = lockerType;
    }

    public void Dispose()
    {
        try
        {
            switch (_lockerType)
            {
                case LockerType.Read:
                    _locker.ExitReadLock();
                    break;

                case LockerType.Write:
                    if (_locker.IsWriteLockHeld)
                    {
                        _locker.ExitWriteLock();

                    }
                    break;
            }
        }
        catch (Exception ex)
        {

        }

    }
}

public enum LockerType
{
    Read,
    Write
}


