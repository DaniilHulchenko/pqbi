namespace PQBI.Infrastructure.Lockers;

public class ArrayLockerSlim : SafeEntityLockerSlim<string[]>
{
    private int _position;
    public ArrayLockerSlim(int capacity)
        : base(new string[capacity])
    {
        Capacity = capacity;
        _position = 0;
    }

    public int Capacity { get; }

    public void Add(string item)
    {
        Do(a =>
        {
            _item[_position] = item;

            if (++_position % Capacity == 0)
            {
                _position = 0;
            }

        });
    }

    public string[] Peek(int lastAmoutLog)
    {
        int amountLogs = Math.Min(lastAmoutLog, Capacity);
        var buffer = new string[amountLogs];

        Do(x =>
        {
            var position = _position - 1;
            for (int index = 0; amountLogs > 0; amountLogs--, position--, index++)
            {
                if (position == -1)
                {
                    position = Capacity - 1;
                }

                buffer[index] = _item[position];
            }
        });


        return buffer;
    }

}
