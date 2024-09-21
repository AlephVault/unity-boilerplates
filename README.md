# unity-boilerplates
This project contains utilities to generate boilerplate code for a project.

# Install

This package is not available in any UPM server. You must install it in your project like this:

1. In Unity, with your project open, open the Package Manager.
2. Either refer this Github project: https://github.com/AlephVault/unity-boilerplates.git or clone it locally and refer it from disk.
3. This package has no extra dependencies.

# Usage

The purpose of this package is to provide a template system that's useful to generate code boilerplates (Behaviours, Scriptable Objects, and other custom classes -- but mostly code in C#).

This is an editor-only package, providing no features for the runtime, since there's nothing actually needed in runtime but only some sort of code assistant for game developers in editor time.

## Utilities

All the relevant classes are into namespace: `AlephVault.Unity.Boilerplates.Utils`.

### `Boilerplate`

This class is actually the main boilerplate applier. It's not meant to apply a single boilerplate but **many** of them at once, as determined by the user via a specific sort of DSL for that purpose. You can create one like this:

```
using AlephVault.Unity.Boilerplates.Utils;

var boilerplate = new Boilerplate();
```

Then, you can use it like this:

```
boilerplate  // By default pointing to the in-project Assets directory.
    .IntoDirectory("Scripts", false)  // Now, into Assets/Scripts. It will complain if Assets/Scripts is not found (because of `false` in the second argument).
        .IntoDirectory("Client", false)  // Now, into Assets/Scripts/Client. It will complain if Assets/Scripts/Client is not found (same reason).
            .Do(SomeCallbackHere)  // Invoking a specific callback into the Assets/Scripts/Client directory.
        .End() // Leaving the Client directory, back to Assets/Scripts.
        .IntoDirectory("Server", false)  // Now, into Assets/Scripts/Server. It will complain if Assets/Scripts/Server is not found (same reason).
            .Do(SomeOtherCallbackHere); Invoking a specific callback into the Assets/Scripts/Server directory.
        .End() // Leaving the Server directory, back to Assest/Scripts.
        .IntoDirectory("Helpers")  // Now, into Assets/Scripts/Helpers. If that directory does not exist, will be created (because no second argument, which defaults to `true`).
            .Do(SomeCallbackHereForHelpers); // Executing another callback into Assets/Scripts/Helpers.
        .End() // Leaving the Helpers directory, back to Assets/Scripts.
        .IntoDirectory("DataModels", true)  // Now, into Assets/Scripts/DataModels. If that directory does not exist, will be created (because of `true` in the second argument).
            .Do(SomeCallbackHereForDataModels, SomeOtherCallbackHereForDataModels, ...);
        .Ennd() // Leaving the DataModels directory, back to Assets/Scripts.
    .End(); // Leaving the Scripts directory, back to Assets.
```

So far, ths boilerplate is designed to arbitrarily do something in those 4 directories under Assets/Scripts. But... what are those callbacks?

It happens that the `.Do` method takes _one or many_ callbacks (variadic argument) that look like this:

```
private void SomeCallbeckHere(Boilerplate theBoilerplate, string currentDirectory) {
   // In this example, `currentDirectory` will be `"Assets/Scripts/Client"`.
   // The variable `theBoilerplate` might not be that useful but otherwise tracks the current boilerplate instance.
}
```

The implementation may be arbitrary (you're free to do anything considering the current boilerplate directory) but it's typically used to generate code. This is because there are some method _factories_ coming out of the box that you can use to generate scripts code.

## Callback Factory: `Boilerplate.InstantiateTemplate`.

This is a **static** method in the Boilerplate class that returns _a callback_. It takes a Unity's `TextAsset` object (with the actual content of the template), a name (**without extension**) for the target file being generated, a dictionary with string keys (for the template variables, typically upper-case with underscores) and string keys (the values to assign to each variable), and whether to complain on non-closed template variables or not (in Unity, it's useful to leave this as-is / in `false`).

This is useful to generate not just code but **any** text-based asset (code, text, HTML pages...).

Consider this example for standard text generation:

1. Assume you have a `TextAsset` having this content:

```
Hello my namy is #NAME#.
My hair color is #HAIR_COLOR#.
My age is #AGE#.
```

2. Assume you want to generate, out of that `TextAsset` asset (which is stored in the `userDataTextAsset` variable), a file in the Assets/TeamData directory, using your data. You'll write a piece of code (perhaps an action in the editor menu or a window?) that looks like this:

```
using AlephVault.Unity.Boilerplates.Utils;

var boilerplate = new Boilerplate();

boilerplate
    .IntoDirectory("TeamData") // Create the Assets/TeamData if it does not exist, and move into.
        .Do(Boilerplate.InstantiateTemplate(userDataTextAsset, "MyAwesomeFile", new Dictionary<string, string>() {{"AGE", "39"}, {"NAME", "Yamcha"}, {"HAIR_COLOR", "Black"}}))
    .End(); // Move out of the TeamData directory, back to Assets.
```

The variable `NAME`, `AGE` and `HAIR_COLOR` correspond to what is defined in the temaplte, and nothing else. They must exist, therein, with surrounding numeral/hash signs. Variables that are not defined in the template will just be ignored in the arguments dictionary.

The generated file will have the extension of the original file referenced by the `TextAsset`. For this example, most likely the asset was a `.txt` file, so the generated file becomes: `Assets/TeamData/MyAwesomeFile.txt`. The contents would be:

```
Hello my namy is Yamcha.
My hair color is Black.
My age is 39.
```

## Callback Factory: `Boilerplate.InstantiateScriptCodeTemplate`.

This is, again, a **static** method just like `InstantiateTemplate`, and takes the exact same arguments with the exact same meanings. There's a difference, however:

1. It's mostly intended to generate **code** files.
2. It automatically ads the following variables, derived from the target script file: `NAME` to be `MyTargetFile`, `SCRIPTNAME` to also be `MyTargetFile`, and finally `SCRIPTNAME_LOWER` to instead bt `myTargetFile`.

To have a better understanding, consider some callbacks from the first example:

1. `SomeCallbackHere` could be `Boilerplate.InstantiateScriptCodeTemplate(someClientTextAsset, "SampleClientAsset", new Dictionary<string, string> {{"FOO", "1"}, {"BAR", "2"}})` and the `someClientTextAsset` variable could point to the following `.cs` file:

```
public class #SCRIPTNAME# {
    public int Foo = #FOO#;
    public long Far = #BAR#;
}
```

That file would be generated as `Assets/Scripts/Client/SampleClientAsset.cs` with the final contents:

```
public class SampleClientAsset {
    public int Foo = 1;
    public long Far = 2;
}
```

Other callback would have a similar implementation.
