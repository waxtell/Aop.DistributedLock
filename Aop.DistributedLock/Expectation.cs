using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;
using Newtonsoft.Json;

namespace Aop.DistributedLock;

public class Expectation<T>
{
    private readonly string _methodName;
    private readonly Type _returnType;
    private readonly Type _instanceType;
    private readonly IEnumerable<Parameter> _parameters;
    private readonly Func<IInvocation, string> _keyFactory;
    public delegate string KeyFactoryDelegate(T instance, object[] @params);

    private Expectation(string methodName, Type returnType, IEnumerable<Parameter> parameters, KeyFactoryDelegate? keyFactory)
    {
        _instanceType = typeof(T);
        _methodName = methodName;
        _returnType = returnType;
        _parameters = parameters;

        if (keyFactory != null)
        {
            _keyFactory = invocation => keyFactory
            (
                (T) invocation.InvocationTarget,
                invocation.Arguments
            );
        }
        else
        {
            _keyFactory = invocation =>
            {
                return
                    JsonConvert
                        .SerializeObject
                        (
                            new
                            {
                                TypeName = invocation.InvocationTarget.GetType().AssemblyQualifiedName,
                                MethodName = invocation.Method.Name,
                                ReturnType = invocation.Method.ReturnType.Name,
                                Arguments = GetArguments()
                            }
                        );

                IEnumerable<object> GetArguments()
                {
                    for (var i = 0; i < invocation.Arguments.Length; i++)
                    {
                        if (_parameters.ElementAt(i).IsEvaluated())
                        {
                            yield return invocation.Arguments[i];
                        }
                    }
                }
            };
        }
    }

    private static Parameter ToParameter(Expression element)
    {
        if (element is ConstantExpression expression)
        {
            return Parameter.MatchExact(expression.Value);
        }

        if (element is MethodCallExpression methodCall && methodCall.Method.DeclaringType == typeof(It))
        {
            switch (methodCall.Method.Name)
            {
                case nameof(It.IsIgnored):
                    return Parameter.Ignore();
                case nameof(It.IsAny):
                    return Parameter.MatchAny();
                case nameof(It.IsNotNull):
                    return Parameter.MatchNotNull();
            }
        }

        return 
            Parameter
                .MatchExact
                (
                    Expression
                        .Lambda(Expression.Convert(element, element.Type))
                        .Compile()
                        .DynamicInvoke()
                );
    }

    public static Expectation<T> FromMethodCallExpression(MethodCallExpression expression, KeyFactoryDelegate? keyFactory)
    {
        return new Expectation<T>
        (
            expression.Method.Name,
            expression.Method.ReturnType,
            expression.Arguments.Select(ToParameter).ToArray(),
            keyFactory
        );
    }

    public static Expectation<T> FromMemberAccessExpression(MemberExpression expression, KeyFactoryDelegate? keyFactory)
    {
        var propertyInfo = (PropertyInfo) expression.Member;

        return new Expectation<T>
        (
            propertyInfo.GetMethod.Name,
            propertyInfo.PropertyType,
            new List<Parameter>(),
            keyFactory
        );
    }

    public bool IsHit(IInvocation invocation)
    {
        return
            IsHit
            (
                invocation.TargetType,
                invocation.Method.Name,
                invocation.Method.ReturnType,
                invocation.Arguments
            );
    }

    public bool IsHit(Type targetType, string methodName, Type returnType, object[] arguments)
    {
        if (!_instanceType.IsAssignableFrom(targetType) || methodName != _methodName || returnType != _returnType)
        {
            return false;
        }

        if (arguments.Length != _parameters.Count())
        {
            return false;
        }

        for (var i = 0; i < arguments.Length; i++)
        {
            if (!_parameters.ElementAt(i).IsMatch(arguments[i]))
            {
                return false;
            }
        }

        return true;
    }

    public string GetLockKey(IInvocation invocation)
    {
        return 
            _keyFactory(invocation);
    }
}