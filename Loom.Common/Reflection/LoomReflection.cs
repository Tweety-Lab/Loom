
using System.Reflection;

namespace Loom.Common.Reflection;

/// <summary>
/// Utility for reflecting.
/// </summary>
public static class LoomReflection
{
    /// <summary> Gets all types with the given attribute. </summary>
    public static IEnumerable<Type> GetTypesWithAttribute<T>(Assembly assembly) => assembly.GetTypes().Where(t => t.IsDefined(typeof(T), true));

    /// <summary> Gets all instances of types with the given attribute. </summary>
    public static IEnumerable<object> InstansiateAllWithAttribute<T>(Assembly assembly, params object[] args) => GetTypesWithAttribute<T>(assembly).Select(t => Activator.CreateInstance(t, args)).Select(t => t!);
}
