# Divar UWP

A Persian, right-to-left Universal Windows Platform client for browsing Divar listings. The application targets Windows 10 version 1703 (build 15063) and includes an ARM configuration intended for Windows 10 Mobile.

## Features

- Browse cities and categories
- View post lists and detailed listing information
- Search listings and apply filters
- View image galleries and full-screen images
- Sign in with a mobile number and verification code
- Reveal contact information and start phone calls
- Bookmark listings
- View the “My Divar” area
- Persian RTL interface

## Requirements

- Windows 10
- Visual Studio with the Universal Windows Platform development workload
- Windows 10 SDK 10.0.15063.0
- NuGet package restore access

## Build and run

1. Open `Divar_UWP.sln` in Visual Studio.
2. Restore NuGet packages.
3. Select the required configuration and architecture, such as `Debug | x86` for local testing or `Debug | ARM` for a compatible mobile device.
4. Choose a deployment target.
5. Build and run the solution.

The project uses `Microsoft.NETCore.UniversalWindowsPlatform` version `6.2.8`. ARM Debug and Release builds use the .NET Native toolchain and full deployment to avoid stale runtime files on Windows 10 Mobile.

## Project structure

- `Views/` — XAML pages and their code-behind
- `ViewModels/` — presentation logic and UI state
- `Models/` — application data models
- `Services/` — authentication, posts, categories, search, filters, bookmarks, contacts, and My Divar operations
- `Infrastructure/` — API access, navigation, credentials, caching, diagnostics, and shared utilities
- `Converters/` — XAML value converters
- `Assets/` — application icons, tiles, and splash-screen images

## Notes

- The application requires internet access to communicate with Divar services.
- Authentication credentials are stored through the Windows Password Vault implementation.
- This is an independent client project and is not an official Divar application.
