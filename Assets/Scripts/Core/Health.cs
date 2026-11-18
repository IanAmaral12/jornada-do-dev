using System;

public class Health
{
    public int Max { get; }
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    public event Action<int, int> Changed;

    public Health(int max)
    {
        Max = max;
        Current = max;
    }

    public void TakeDamage(int amount)
    {
        Current = Math.Max(0, Current - amount);
        Changed?.Invoke(Current, Max);
    }

    public void Reset()
    {
        Current = Max;
        Changed?.Invoke(Current, Max);
    }
}
