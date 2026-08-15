using System;
using UniAutoRef;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AutoRefAttribute : Attribute
{
    public FindIn Find { get; }

    /// <summary>
    /// The attribute for auto-find components.
    /// </summary>
    /// <param name="findIn">Where try find?</param>
    public AutoRefAttribute(FindIn findIn = FindIn.Scene)
    {
        Find = findIn;
    }
}
