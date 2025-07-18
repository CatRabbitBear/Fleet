This is a proof of concept to see if a Blazor app could be hosted and live in the system tray. 

The Blazor within a WPF is a bit of mess but does publish and run. 

Heres some gotchas 
- Fleet.Tray is the main entrypoint
- You will need an Azure Foundry Project with model deployed and API key generated
- These are asked for upon launch, not supplying closes the program
- Even when running in IDE close the program by right-clicking on the tray icon and clicking exit to allow full clean-up
- The Blazor hosting is like an onion with IHost in Fleet.Tray finally unwrapping as an IWebHost.

This is how the Blazor trail unravvels
- `Fleet.Tray.App.xaml.cs` maintains the reference to the `IHost _webHost`
- `_webHost` is created by calling a method `Fleet.Blazor.BlazorHostBuilder.cs` which returns an `IHostBuilder`
- Inside `BlazorHostBuilder` you will see a method in tha chain called `.ConfigureWebHostDefaults` and everything inside here is fairly similar to what would be in `program.cs` in a template project.
- The notable difference from the modern API is the `Startup.cs` in the Blazor project which has some old style configuration.
