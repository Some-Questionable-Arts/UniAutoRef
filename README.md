### UniAutoRef

### What is this?

**UniAutoRef is a lightweight, high-performance C# Source Generator designed for Unity. It automatically detects and resolves component references in your scripts at compile-time, completely eliminating boring boilerplate code.**

### What problem does it solve?

When writing scripts in Unity, you frequently need to get and store references to other components (for example, the Rigidbody or Animator on a Player). Typically, developers solve this in two ways: 

1. **Using `GetComponent<T>()` in `Awake()` / `Start()` — This results in writing dozens of repetitive, messy lines of code for every script and often requires extra debug logs to trace missing components.**
2. **Dragging and dropping components via the Inspector — This is time-consuming, breaks easily if prefabs are modified, and slows down your workflow.**

**UniAutoRef solves this entirely. It automates reference management behind the scenes, keeping your codebase clean, safe, and fast without adding any runtime overhead.**

### **And this how to use it:**

# **Step 0 - Downoload.**

**You can install this package via the Unity Package Manager using its Git URL.**
**Follow these simple steps: in Unity, click "Window -> Package Manager -> + -> Add package from git URL..." and paste this URL: `https://github.com/Some-Questionable-Arts/UniAutoRef.git` like this:**

<img width="906" height="127" alt="image" src="https://github.com/user-attachments/assets/1b67d9fe-0144-4393-96a8-73a426daa9c7" />

**And click "Add".**

# **Step 1 - Set up**

**To get started, make your class partial where you intend to use the `[AutoRef]` attribute. It should look something like this:**

```csharp
using UnityEngine;

public partial class TestScript : MonoBehaviour
{
    [AutoRef] public BoxCollider _bx;
}
```

**The attribute takes 1 argument: UniAutoRef.FindIn (enum, default = scene). This allows you to choose WHERE to search. The options are: 
FindIn.Scene, FindIn.Children, FindIn.Parent, FindIn.Self**

> [!IMPORTANT]
> **This is required for the field to be serialized and displayed in the Inspector.**

# **Step 2 - Use**

**Now, let's say you saved the script and attached it to an object in the scene. To save all references, click: Tools -> UAR -> Find All References. If something is missing, you will see a console message showing what was not found and where.** 

**Thats it. Nothing more. Just use it.**
