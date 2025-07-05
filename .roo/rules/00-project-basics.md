This solution is using the AspNet Blazor Web App approach.

# Guidelines for the entire solution

Do not use `InteractiveServer` render mode. Do not use `InteractiveAuto` render mode.

This solution is using the Havit.Blazor UI library. Use Context7 for Havit.Blazor docs.
Prefer using Havit.Blazor components. Otherwise, fall back to raw Bootstrap 5.3 HTML and CSS.

Prefer bootstrap CSS helper classes over CSS files.

Prefer `[AutoConstructor]` attribute or primary constructor for classes that require dependency injection.
Avoid manually writing constructors for classes that require dependency injection.

Prefer using `[RegisterSingleton]`, `[RegisterScoped]`, or `[RegisterTransient]` attributes for dependency injection registration.

# `Namezr` project guidelines

All code in the `Namezr` project must use static rendering.

Code in the `Namezr` project should follow the following structure:

* `[Feature]/[Type]/[ClassName].cs` for most classes.
* `[Feature]/[Type]/[ClassName].razor` for Blazor pages and components.
* `Infrastructure/[Category]/` for code that spans multiple features or is not feature-specific.
* `Helpers/[Category]` for miscellaneous helper code that is not feature-specific.

HTTP endpoints are implemented using the Immediate.Apis library. Use Context7 for Immediate.Apis docs.

# `Namezr.Client` project guidelines

Code that requires interactivity should live in `Namezr.Client` and use `InteractiveWebAssembly` render mode.

# Code style guidelines

Use the following code style guidelines:

* Use trailing commas in object and collection initializers.
* Prefer explicitly typed variables over `var` unless the type is longer than 30 characters.