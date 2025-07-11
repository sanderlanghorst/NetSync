using System.Collections.Concurrent;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NetSync.Protos;

namespace NetSync;

public class SyncData : ISyncData
{
    private static class Headers
    {
        public const string Action = "action";
        public const string Type = "type";

        public static class Actions
        {
            public const string Remove = "remove";
            public const string Sync = "sync";
            public const string Full = "full";
        }
    }

    private readonly IMessaging _messaging;
    private readonly ISerializer _serializer;
    private readonly ILogger<SyncData> _logger;

    private ConcurrentDictionary<string, object> Data { get; set; }

    public SyncData(IMessaging messaging, ISerializer serializer, ILogger<SyncData> logger)
    {
        _messaging = messaging;
        _serializer = serializer;
        _logger = logger;
        Data = new ConcurrentDictionary<string, object>();
        _messaging.OnMessageReceived += MessagingOnOnMessageReceived;
    }

    public void HandleProtocol(IMessage message)
    {
        switch (message)
        {
            case Data sync:
                HandleSyncData(sync);
                break;
            default: return;
        }
    }

    private void HandleSyncData(Data sync)
    {
        var action = sync.Headers.FirstOrDefault(h => h.Key == Headers.Action);
        if (action is null) return;
        switch (action.Value)
        {
            case Headers.Actions.Remove:
            {
                foreach (var kvp in sync.Data_)
                {
                    Data.TryRemove(kvp.Key, out _);
                }

                break;
            }
            case Headers.Actions.Sync:
            {
                var type = Type.GetType(sync.Headers.First(h => h.Key == Headers.Type).Value);
                if (type == null) break;
                foreach (var kvp in sync.Data_)
                {
                    var value = kvp.Value.ToByteArray();
                    Data[sync.Key] = _serializer.Deserialize(value, type);
                }

                break;
            }
            case Headers.Actions.Full:
            {
                var type = Type.GetType(sync.Headers.First(h => h.Key == Headers.Type).Value);
                if (type == null) break;
                foreach (var kvp in sync.Data_)
                {
                    var key = kvp.Key;
                    var value = kvp.Value.ToByteArray();
                    Data[key] = _serializer.Deserialize(value, type);
                }

                break;
            }
        }
    }


    private void MessagingOnOnMessageReceived(object? sender, IMessage e)
    {
        _logger.LogTrace($"Sync message received: {e.GetType().Name}");
        HandleProtocol(e);
    }

    public void Set<T>(string key, T? value)
    {
        if (value == null)
        {
            Data.TryRemove(key, out _);
            var message = GetRemoveMessage(key);
            _messaging.Send(message, CancellationToken.None);
        }
        else
        {
            Data.AddOrUpdate(key, value, (k, v) => value);
            var message = SerializeMessage(key, value);
            _messaging.Send(message, CancellationToken.None);
        }
    }

    private Data SerializeMessage<T>(string key, T value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        return GetSyncMessage(key, [_serializer.Serialize(value)], value.GetType());
    }

    private Data GetSyncMessage(string key, byte[][] data, Type type)
    {
        var message = new Data
        {
            Key = key,
            Timestamp = (ulong)DateTime.Now.Ticks,
            Headers =
            {
                new DataHeader
                {
                    Key = Headers.Action,
                    Value = Headers.Actions.Sync
                },
                new DataHeader
                {
                    Key = Headers.Type,
                    Value = type.AssemblyQualifiedName
                }
            },
        };
        message.Data_.AddRange(data.Select((b, i) => new Kvp { Key = i.ToString(), Value = ByteString.CopyFrom(b) }));
        return message;
    }


    private Data GetRemoveMessage(string key)
    {
        return new Data
        {
            Key = key,
            Timestamp = (ulong)DateTime.Now.Ticks,
            Headers =
            {
                new DataHeader
                {
                    Key = Headers.Action,
                    Value = Headers.Actions.Remove
                }
            }
        };
    }

    public T? Get<T>(string key)
    {
        if (Data.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        return default;
    }

    public ICollection<string> List()
    {
        return Data.Keys;
    }

    public void Clear()
    {
        Data.Clear();
    }
}