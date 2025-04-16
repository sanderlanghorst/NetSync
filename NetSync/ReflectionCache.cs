using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NetSync;

public static class ReflectionCache
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, (Type, Func<object, object>)>> PropertyGetters = new();

    public static IDictionary<string, (Type propertyType, Func<object, object> invocation)> GetGetters(Type type)
    {
        return PropertyGetters.GetOrAdd(type, CreatePropertyGetters);
    }
    
    private static Dictionary<string, (Type propertyType, Func<object, object> invocation)> CreatePropertyGetters(Type type)
    {
        var propertyGetters = new Dictionary<string, (Type, Func<object, object>)>();

        foreach (var property in type.GetProperties())
        {
            if (!property.CanRead) continue;

            var parameterExpression = Expression.Parameter(typeof(object), "obj");
            var propertyAccess = Expression.Property(Expression.Convert(parameterExpression, type), property.Name);
            var lambda = Expression.Lambda<Func<object, object>>(Expression.Convert(propertyAccess, typeof(object)), parameterExpression);

            propertyGetters[property.Name] = (property.PropertyType, lambda.Compile());
        }

        return propertyGetters;
    }
}