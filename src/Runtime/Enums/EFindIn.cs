using System;

namespace UniAutoRef
{
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

        /// <summary>
        /// Finds in scene. Uses: FindFirstObjectByType<...>()
        /// </summary>
        Scene,
    }
}
