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

**To get started, make sure the class where you intend to use this package is marked as `partial`. You also need to declare a `partial` function named `GeneratedAwake` (keep in mind: the package is strictly case-sensitive, so it must be spelled exactly like this) and call it at the very beginning of your `Awake method`. It should look something like this:**

```csharp
using UnityEngine;

public partial class TestScript : MonoBehaviour
{
    partial void GeneratedAwake();

    private void Awake()
    {
        GeneratedAwake();
    }
}
```

# **Step 2 - Use it!**

**That's it, the setup is complete! Now you can start using the [AutoFind] attribute! Here are more details about this attribute:**

**It takes 2 arguments: Debug (enum. Default = Debug.Enabled), FindIn (enum. Default = FindIn.Self).** 

### **1st Argument:**
**`Debug.Disable` - Disables debug mode (if the component is not found, nothing will be printed to the console). `Debug.Enable` - Enables debug mode (if the component is not found, you will see the following in the console: "`[AutoFind]: The {variable name} in {class name} (Instance: {Name in hierarchy}) is not found.`").**

**It is recommended to leave this as default (Debug.Enable). It will be stripped from the final build of the game via preprocessor directives (#if).**

### **2nd Argument:**
**`FindIn.Self` (will call `GetComponent<...>();`), `FindIn.Children` (will call `GetComponentInChildren<...>();`), `FindIn.Parent` (will call `GetComponentInParent<...>();`), `FindIn.Scene` (will call `FindFirstObjectByType<Type>();`).**

**Here you choose where to search.** 
> [!IMPORTANT]
> **These arguments require the `UniAutoRef` namespace.**
