# Contributing - Csharp (Blazor) User Interface for TestStand
Thank you for contributing to TestStand Blazor User Interface. This document gives a step‑by‑step guidance for local development, packaging, and making contributions.

## Getting started

To submit changes to TestStand Blazor UI, the first step is to build the repo which requires the following to be setup:

- Sync the repository
- Install Node.js version 24+ (run `node --version`) and npm version 10+ (run `npm --version`) which can be downloaded from <https://nodejs.org/en/download/>
- Install .NET 8 SDK which can be downloaded from <https://dotnet.microsoft.com/en-us/download>
   - Run `dotnet --info` to verify the required version of the SDK is installed.
   
## Setting up backend dependencies
- Install TestStand 2025 Q3 or later and license the product.
- Install Sequencing Service package, from the release page.
- Restart the machine.
- Open TestStand Version Selector and verify that it shows the previously installed version of TestStand as active TestStand version.
- If not, set the active version to the installed TestStand version.

## Building and running the application

From the `TestStand BlazorUI` source directory:

1. Navigate to electron directory - `cd electron`
2. Run `npm install`
3. Run `npm run start:dev` to run the application in development mode.


## Debugging

- You can find the logs for the BlazorUI application in `%ProgramData%/National Instruments/TestStand/Logs`.
- The logs for the backend sequencing service in `%ProgramData%/National Instruments/Sequencing Service/Logs`.

## Troubleshooting

- If you face any unexpected issues in the application and restarting the application doesn't work, try out the below steps:
    - Launch Task Manager.
    - Look for `NationalInstruments.SequencingService` process and kill it manually.
    - Try restarting the WebOI application and see if your problem resolves.
    - If the problem still persists, raise an issue on GitHub.
