using System.Collections;

namespace Convex.Client.Shared.Validation;

/// <summary>
/// Runtime schema validator that performs type checking on response values.
/// </summary>
public sealed class RuntimeSchemaValidator : ISchemaValidator
{
    /// <inheritdoc/>
    public SchemaValidationResult Validate<TExpected>(
        object? value,
        string functionName,
        SchemaValidationOptions options) => Validate(value, typeof(TExpected), functionName, options);

    /// <inheritdoc/>
    public SchemaValidationResult Validate(
        object? value,
        Type expectedType,
        string functionName,
        SchemaValidationOptions options)
    {
        if (expectedType == null)
        {
            throw new ArgumentNullException(nameof(expectedType));
        }

        var errors = new List<string>();
        var actualType = value?.GetType();
        var expectedTypeName = GetTypeName(expectedType);
        var actualTypeName = actualType != null ? GetTypeName(actualType) : "null";

        // Null handling
        if (value == null)
        {
            if (IsNullableType(expectedType))
            {
                return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
            }
            else
            {
                errors.Add($"Expected non-null value of type {expectedTypeName}, but got null");
                return SchemaValidationResult.Failure(expectedTypeName, actualTypeName, errors);
            }
        }

        // Exact type match
        if (actualType == expectedType)
        {
            return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
        }

        // Assignable type match (for non-strict mode)
        if (!options.StrictTypeChecking && expectedType.IsAssignableFrom(actualType))
        {
            return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
        }

        // Nullable type handling
        var underlyingExpectedType = Nullable.GetUnderlyingType(expectedType);
        if (underlyingExpectedType != null)
        {
            if (actualType == underlyingExpectedType)
            {
                return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
            }

            if (!options.StrictTypeChecking && underlyingExpectedType.IsAssignableFrom(actualType))
            {
                return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
            }
        }

        // Collection type validation
        if (actualType != null && IsCollectionType(expectedType) && IsCollectionType(actualType))
        {
            var expectedElementType = GetElementType(expectedType);
            var actualElementType = GetElementType(actualType);

            if (expectedElementType != null && actualElementType != null)
            {
                if (expectedElementType == actualElementType)
                {
                    return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
                }

                if (!options.StrictTypeChecking && expectedElementType.IsAssignableFrom(actualElementType))
                {
                    return SchemaValidationResult.Success(expectedTypeName, actualTypeName);
                }

                errors.Add($"Collection element type mismatch: Expected {GetTypeName(expectedElementType)}, got {GetTypeName(actualElementType)}");
                return SchemaValidationResult.Failure(expectedTypeName, actualTypeName, errors);
            }
        }

        // Type mismatch
        errors.Add($"Type mismatch: Expected {expectedTypeName}, got {actualTypeName}");
        return SchemaValidationResult.Failure(expectedTypeName, actualTypeName, errors);
    }

    private static bool IsNullableType(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    private static bool IsCollectionType(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    private static Type? GetElementType(Type collectionType)
    {
        // Array
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        // Generic collection (List<T>, IEnumerable<T>, etc.)
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                return genericArgs[0];
            }
        }

        // Check implemented interfaces for IEnumerable<T>
        foreach (var iface in collectionType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return GetTypeName(Nullable.GetUnderlyingType(type)!) + "?";
        }

        if (type.IsGenericType)
        {
            var genericName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
            return $"{genericName}<{genericArgs}>";
        }

        return type.Name;
    }
}
