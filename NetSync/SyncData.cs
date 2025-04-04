using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using NetSync.Protos;

namespace NetSync;

public class SyncData : ISyncData
{
    private readonly IMessaging _messaging;

    public ConcurrentDictionary<string, WeakReference<object>> Data { get; set; }

    public SyncData(IMessaging messaging)
    {
        _messaging = messaging;
        Data = new ConcurrentDictionary<string, WeakReference<object>>();
    }

    public void Register(string key, object value)
    {
        if (Data.ContainsKey(key))
        {
            Data[key].SetTarget(value);
        }
        else
        {
            Data.TryAdd(key, new WeakReference<object>(value));
        }

        switch (value)
        {
            case INotifyCollectionChanged collectionChanged:
                collectionChanged.CollectionChanged += async (sender, args) =>
                {
                    var message =
                        new MessageSync
                        {
                            Key = key
                        };
                    message.Data.AddRange(GetData(args));
                    await _messaging.Send(message, default);
                };
                break;
            case INotifyPropertyChanged propertyChanged:
                propertyChanged.PropertyChanged += async (sender, args) =>
                {
                    var message = new MessageSync
                    {
                        Key = key
                    };
                    message.Data.AddRange(GetData(args));
                    await _messaging.Send(message, default);
                };
                break;
        }
    }

    private IEnumerable<Kvp> GetData(PropertyChangedEventArgs args)
    {
        return new List<Kvp>();
    }

    private IEnumerable<Kvp> GetData(NotifyCollectionChangedEventArgs args)
    {
        return new List<Kvp>();
    }
}