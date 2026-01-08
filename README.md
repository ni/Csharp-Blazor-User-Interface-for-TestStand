# Csharp (Blazor) User Interface for TestStand - Early Access Release

This solution provides a modern-looking, scalable and maintainable User Interface for TestStand. A TestStand User Interface is an application that provides a graphical interface for executing tests at a production station, to monitor, debug and control execution of TestStand sequences. You can modify the source code and customize the user interface to suit your specific testing needs.

This user interface is built using Blazor framework targeting .NET 8. It uses new C# UI controls for TestStand, that no longer use ActiveX. It uses electron to host the application on a Windows desktop.
For more details on TestStand user interfaces & best practices, refer [here](https://www.ni.com/en/support/documentation/supplemental/08/teststand-user-interface-development-best-practices.html?srsltid=AfmBOooCU7uu-z2wDBqMuy9KzPsJ2kntJMtzasA3YiJuRyxyQ508o3ev).

## Getting Started

- Install TestStand 2025 or later in your machine and license it.
- Install the `Sequencing Service` and `TestStand BlazorUI` application in your machine using the latest installers provided in the [release page](https://github.com/ni/Csharp-Blazor-User-Interface-for-TestStand/releases).
- After installing Sequencing Service, open TestStand Version Selector and verify that it shows the previously installed version of TestStand as active TestStand version. Else, switch to that version.
- Restart the machine.
- Now, open `TestStandBlazorUI.exe` from Start menu or from the installed location `C:\Program Files\National Instruments\TestStand\Blazor UI`

## Default Credentials

If the default user credentials in TestStand is not modified, use the following credentials:

Username: `administrator`

Password: `teststand`

## Troubleshooting

- If you face any unexpected issues in the application and restarting the application doesn't work, try out the below steps:
    - Launch Task Manager.
    - Look for `NationalInstruments.SequencingService` process and kill it manually.
    - Try restarting the BlazorUI application and see if your problem resolves.
    - If the problem still persists, raise an issue on GitHub.

## Contributing

See `Getting Started` in [`Contributing.md`](/CONTRIBUTING.md#getting-started) to get started with building the repository.
