namespace Ghost.Core;

public ref struct Ref<T>
{
    private ref T _value;

    public ref T Value
    {
        get => ref _value;
    }

    public Ref(ref T value)
    {
        _value = ref value;
    }
}