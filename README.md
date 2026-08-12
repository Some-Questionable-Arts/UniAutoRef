> [!WARNING]
> Dont downoload the current version (0.9.2) it has a couple of errors. It will be fixed soon.


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

**It takes exactly 1 argument, which is the UniAutoRef.Debug (`enum`, default - `UniAutoRef.Debug.Enable`). You can choose between 2 options:** 

**`UniAutoRef.Debug.Enable` and `UniAutoRef.Debug.Disable`**

**With the Disable option... nothing extra will happen. It will just automatically search for components silently (which is not recommended during development).**

**With the Enable option, if `[AutoFind]` fails to find a component, it will print a warning to the console (`Debug.Log`) showing exactly which script missed the component and which specific variable it was supposed to be assigned to.**

```csharp
using UnityEngine;

public partial class TestScript : MonoBehaviour
{
    partial void GeneratedAwake();

    [AutoFind(UniAutoRef.Debug.Enable)] private Rigidbody _rb;

    private void Awake()
    {
        GeneratedAwake();
        // if _rb is null (not found) - in console you will see: "[AutoFind] The _rb in TestScript is not found."
    }
}
```
**(Since version `0.9.2`, you don't need to manually disable debugging for each attribute in the final build — it will not be included in the release build. It will only run in the Editor and debug builds.)**
