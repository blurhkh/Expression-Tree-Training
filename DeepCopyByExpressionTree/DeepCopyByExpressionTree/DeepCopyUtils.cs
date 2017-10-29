using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DeepCopyByExpressionTree
{
    public class DeepCopyUtils
    {
        // 委托缓存
        private static ConcurrentDictionary<(Type, Type), MulticastDelegate> _caches;

        static DeepCopyUtils() => _caches = new ConcurrentDictionary<(Type, Type), MulticastDelegate>();

        public static TTarget Copy<TSource, TTarget>(TSource source)
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var key = (sourceType, targetType);
            if (_caches.TryGetValue(key, out var copy))
            {
                return (TTarget)copy.DynamicInvoke(source);
            }
            else
            {
                var parameterExpression = Expression.Parameter(sourceType, nameof(source));

                var memberBindings = new List<MemberBinding>();

                var copyMethodInfo = typeof(DeepCopyUtils).GetMethod(nameof(Copy));

                foreach (var targetPropInfo in targetType.GetProperties())
                {
                    var sourcePropInfo
                        = sourceType.GetProperty(targetPropInfo.Name);

                    var sourcePropType = sourcePropInfo?.PropertyType;
                    var targetPropType = targetPropInfo.PropertyType;
                    if (sourcePropType != targetPropType
                        || !sourcePropInfo.CanRead
                        || !targetPropInfo.CanWrite)
                    {
                        continue;
                    }

                    Expression expression = Expression
                            .Property(parameterExpression, sourcePropInfo);
                    if (!targetPropType.IsValueType && targetPropType != typeof(string))
                    {
                        expression = Expression.Call(null,
                            copyMethodInfo.MakeGenericMethod(sourcePropType, targetPropType), expression);
                    }
                    memberBindings.Add(Expression.Bind(targetPropInfo, expression));
                }

                var memberInitExpression = Expression
                    .MemberInit(Expression.New(targetType), memberBindings);

                copy = Expression.Lambda<Func<TSource, TTarget>>(
                    memberInitExpression, parameterExpression).Compile();

                _caches.TryAdd(key, copy);
                return (TTarget)copy.DynamicInvoke(source);
            }
        }
    }
}
