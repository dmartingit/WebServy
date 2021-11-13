namespace WebServy.Data;

public sealed class Observable<T>
{
    private T? value;

    public Observable(T? value = default)
    {
        Value = value;
    }

    public event ChangedEventHandler Changed = delegate { };
    public T? Value
    {
        get => value;
        set
        {
            this.value = value;
            Changed.Invoke(this, new() { Value = value });
        }
    }

    public class ChangedEventArgs : EventArgs
    {
        public T? Value { get; set; }
    }

    public delegate void ChangedEventHandler(object sender, ChangedEventArgs e);
}
