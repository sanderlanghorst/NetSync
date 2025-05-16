namespace NetSync;

public interface ISyncData
{
    void Set<T>(string key, T? value);
    T? Get<T>(string key);
    ICollection<string> List();

    void Clear();
}