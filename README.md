# TestStand-WebOI

This solution is a modular Blazor web application targeting .NET 8, designed for TestStand operator interface (OI) with Sequencing Service as its backend. It leverages reusable components, gRPC-based backend integration, and dynamic theming to deliver a modern, maintainable, and scalable user experience.

## Getting Started

- Install TestStand 2025 or later in your machine and license it.
- Install the `Sequencing Service` and `TestStand WebOI` application in your machine using the latest installers provided in the [release page](https://github.com/ni/TestStand-WebOI/releases).
- After installing Sequencing Service, open TestStand Version Selector and verify that it shows the previously installed version of TestStand as active TestStand version.
- Restart the machine.
- Now, open `TestStand WebOI.exe` from Start menu or from the installed location `C:\Program Files\National Instruments\TestStand WebOI`

## Troubleshooting

- If you face any unexpected issues in the application and restarting the application doesn't work, try out the below steps:
    - Launch Task Manager.
    - Look for `NationalInstruments.SequencingService` process and kill it manually.
    - Try restarting the WebOI application and see if your problem resolves.
    - If the problem still persists, raise an issue on GitHub.

## Contributing

See `Getting Started` in [`Contributing.md`](/CONTRIBUTING.md#getting-started) to get started with building the repository.
