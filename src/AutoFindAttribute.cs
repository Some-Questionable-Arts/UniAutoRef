using System;
using UniAutoRef;

namespace UniAutoRef
{
    /// <summary>
    /// On-off debug. Recomended to enable.
    /// </summary>
    public enum Debug : byte
    {
        [Obsolete("Not recommended. Use Enable instead.")]
        Disable,

        /// <summary>
        /// Enables logging. Logging will not be included in the final build.
        /// </summary>
        Enable,
    }

    /// <summary>
    /// Where to find
    /// </summary>
    public enum FindIn : byte
    {
        /// <summary>
        /// Finds in self. Uses: GetComponent<...>()
        /// </summary>
        Self,

        /// <summary>
        /// Finds in children. Uses: GetComponentInChildren<...>()
        /// </summary>
        Children,

        /// <summary>
        /// Finds in parent. Uses: GetComponentInParent<...>()
        /// </summary>
        Parent,

        [Obsolete("Not recommended due to performance impact. Uses FindFirstObjectByType under the hood.")]
        Scene,
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AutoFindAttribute : Attribute 
{
    public Debug IsDebug { get; }
    public FindIn Find { get; }

    /// <summary>
    /// The attribute for auto-find components.
    /// </summary>
    /// <param name="isDebug">Debug mode.</param>
    /// <param name="findIn">Where try find?</param>
    public AutoFindAttribute(Debug isDebug = Debug.Enable, FindIn findIn = FindIn.Self)
    {
        IsDebug = isDebug;
        Find = findIn;
    }
}
