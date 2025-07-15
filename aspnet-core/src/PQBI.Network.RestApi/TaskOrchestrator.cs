using Microsoft.Extensions.Options;
using PQBI.Infrastructure;
using PQBI.Infrastructure.Extensions;
using PQS.Data.RecordsContainer;

namespace PQBI.Network.RestApi;

public class WaitSignal<TData>
{
    private readonly SemaphoreSlim _startWorkingSignal;

    private TData _data;

    public WaitSignal()
    {
        _startWorkingSignal = new SemaphoreSlim(0);
    }

    public void Set(TData data)
    {
        _data = data;
        _startWorkingSignal.Release();
    }


    public async Task<TData> WaitOneAsync()
    {
        await _startWorkingSignal.WaitAsync();
        return _data;
    }
}

public class TaskOrchestratorConfig : PQSConfig<TaskOrchestratorConfig>
{
    public int MaxTasks { get; set; }
}

public interface ITaskOrchestrator
{
    Task<PQSResponse[]> DispatchBatch(params Func<Task<PQSResponse>>[] callbacks);
}

public class TaskOrchestrator : ITaskOrchestrator
{

    private readonly SemaphoreSlim _startWorkingSignal;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;
    private readonly Task _pollingTask;
    private TaskOrchestratorConfig _config;
    private readonly Queue<(Func<Task<PQSResponse>>[] Delegates, WaitSignal<PQSResponse[]> Responses)> _queue;

    private readonly SemaphoreSlim _invokatioBlocker;

    public TaskOrchestrator(IOptions<TaskOrchestratorConfig> config)
    {
        _config = config.Value;
        _queue = new Queue<(Func<Task<PQSResponse>>[], WaitSignal<PQSResponse[]>)>();

        _invokatioBlocker = new SemaphoreSlim(_config.MaxTasks, _config.MaxTasks);
        _startWorkingSignal = new SemaphoreSlim(0);

        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;

        _pollingTask = Task.Run(PollingTask);
    }

    private async void PollingTask()
    {
        while (_cancellationToken.IsCancellationRequested == false)
        {
            if (_queue.TryDequeue(out (Func<Task<PQSResponse>>[], WaitSignal<PQSResponse[]>) batch))
            {
                var @delegates = batch.Item1;
                if (@delegates != null)
                {
                    var batchRequests = new List<Task<PQSResponse>>();

                    foreach (var @delegate in @delegates)
                    {
                        batchRequests.Add(Task.Run(async () => await @delegate()));
                    }

                    await Task.WhenAll(batchRequests);
                    batch.Item2.Set(batchRequests.Select(x => x.Result).ToArray());
                }

                continue;
            }

            await _startWorkingSignal.WaitAsync();
        }
    }

    public async Task<PQSResponse[]> DispatchBatch(params Func<Task<PQSResponse>>[] callbacks)
    {
        var responses = new List<PQSResponse>();
        //var skip = 0;

        //var list = new List<Func<Task<PQSResponse>>>();
        for (var index = 0; index < callbacks.Length;)
        {
            var waitHandle = new WaitSignal<PQSResponse[]>();

            var batch = callbacks.Skip(index).TakeSafe(_config.MaxTasks).ToArray();
            index += batch.Length;

            _queue.Enqueue((batch, waitHandle));
            _startWorkingSignal.Release();

            var result = await waitHandle.WaitOneAsync();
            responses.AddRange(result);

        }

        return responses.ToArray();
    }
}
