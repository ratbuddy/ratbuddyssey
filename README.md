# Ratbuddyssey

Cross-platform editor for Audyssey MultEQ `.ady` calibration files.

> Originally a fork of [ratbuddy/ratbudyssey](https://github.com/ratbuddy/ratbudyssey).
> Discussion thread: [AVS Forum announcement](https://www.avsforum.com/forum/90-receivers-amps-processors/3006886-announcing-ratbuddyssey-tool-tweaking-audyssey-multeq-app-files.html).

## What it is

Ratbuddyssey is an **offline `.ady` file editor**. It loads a calibration file
exported from the official Audyssey MultEQ Editor app, lets you tweak channel
parameters / target curves / EQ filters, and saves the result back as `.ady`
(JSON) so you can re-import it into the Audyssey app.

It does **not** talk to your AVR over the network. There is no sniffer, no
TCP/IP feature, and no elevated-privilege requirement.

## Status

Early/alpha. Use at your own risk — always keep a backup of your original
`.ady` files.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build) or
  the .NET 8 runtime (to run a framework-dependent build).
- Windows, Linux, or macOS (Intel or Apple Silicon).

## Build & run from source

```sh
git clone https://github.com/ratbuddy/ratbuddyssey.git
cd ratbuddyssey
dotnet run --project Ratbuddyssey
```

## Run the tests

```sh
dotnet test
```

## Publish a self-contained build

```sh
# Pick the runtime that matches your target platform:
dotnet publish Ratbuddyssey/Ratbuddyssey.csproj -c Release -r win-x64   --self-contained
dotnet publish Ratbuddyssey/Ratbuddyssey.csproj -c Release -r linux-x64 --self-contained
dotnet publish Ratbuddyssey/Ratbuddyssey.csproj -c Release -r osx-x64   --self-contained
dotnet publish Ratbuddyssey/Ratbuddyssey.csproj -c Release -r osx-arm64 --self-contained
```

CI also publishes framework-dependent artifacts for `win-x64`, `linux-x64`,
`osx-x64`, and `osx-arm64` on every push — see the [Actions tab](../../actions).

## Tech stack

- [.NET 8](https://dotnet.microsoft.com/) + C# 12
- [Avalonia 11](https://avaloniaui.net/) UI (cross-platform XAML)
- [ScottPlot 5](https://scottplot.net/) for charting
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
  source generators
- [Newtonsoft.Json](https://www.newtonsoft.com/json) for `.ady` serialization
  (preserves the property order the official Audyssey app expects)
- [MathNet.Numerics](https://numerics.mathdotnet.com/) for filter math

## License

[MIT](LICENSE).
