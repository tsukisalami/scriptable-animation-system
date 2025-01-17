// we could use 'System.Reflection', but this solution should be slightly faster
public interface IPreset<T>
{
    public abstract void InitializeValues(T obj);
}