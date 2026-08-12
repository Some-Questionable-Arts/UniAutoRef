using System;
using UniAutoRef;

namespace UniAutoRef
{
    public enum Debug : byte
    {
        Disable,
        Enable,
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AutoFindAttribute : Attribute 
{
    public Debug IsDebug { get; }

    public AutoFindAttribute(Debug isDebug = Debug.Enable)
    {
        IsDebug = isDebug;
    }
}
