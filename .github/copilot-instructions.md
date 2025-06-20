This solution is using the AspNet Blazor Web App approach.

This solution is using the Havit.Blazor UI library.
The documentation can be found at https://havit.blazor.eu
Prefer using Havit.Blazor components. Otherwise, fall back to raw Bootstrap 5.3 HTML and CSS.

Prefer bootstrap CSS helper classes over CSS files.

All code in the `Namezr` project must use static rendering.
Code that requires interactivity should live in `Namezr.Client` and use `InteractiveWebAssembly` render mode.
Do not use `InteractiveServer` render mode. Do not use `InteractiveAuto` render mode.

Code in the `Namezr` project should follow the existing feature-based structure.

HTTP endpoints are implemented using the Immediate.Apis library.
The documentation can be found at https://immediateplatform.dev/docs/Immediate.Apis