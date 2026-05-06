# Ratbuddyssey

Cross-platform editor for Audyssey MultEQ `.ady` calibration files.

> Discussion thread: [AVS Forum announcement](https://www.avsforum.com/forum/90-receivers-amps-processors/3006886-announcing-ratbuddyssey-tool-tweaking-audyssey-multeq-app-files.html).

## What it is

Ratbuddyssey is an **offline `.ady` file editor**. It loads a calibration file
exported from the official Audyssey MultEQ Editor app, lets you tweak channel
parameters / target curves / EQ filters, and saves the result back as `.ady`
(JSON) so you can re-import it into the Audyssey app.

It does **not** talk to your AVR over the network. You always go through the
official MultEQ Editor app for the upload step. That keeps Ratbuddyssey out of
the receiver's authentication / pairing path and means you can experiment
freely without ever risking the device's own state.

## What you get

- **Channels panel** with friendly speaker names (Front L, Top Front R,
  Subwoofer 1, ...) derived from the Audyssey `commandId`. Live filter box on
  top to jump to a channel by name or id.
- **Per-channel hardware-limits validation** with a status dot in the leftmost
  column: green = within the receiver's allowed ranges, yellow = soft warning
  (e.g. crossover off the receiver's snap list), red = the official Editor
  will refuse the value (e.g. trim outside ±12 dB).
- **Hardware quirks summary** in the status strip: detected receiver model,
  speed of sound (300 vs 343 m/s depending on firmware era), remaining
  subwoofer-distance headroom, and the most-negative trim the AVR will still
  accept on the sub channel — all derived from the same heuristics
  [AudysseyOne](https://github.com/ObsessiveCompulsiveAudiophile/AudysseyOne)
  uses.
- **Target-curve editor** with add/remove points and an 8-position
  measurement-slot strip on the chart, using the
  [Wong (2011) colorblind-safe palette](https://www.nature.com/articles/nmeth.1618).
- **Light / Dark / System theme** with persistence between launches.
- **Recent files** menu (up to 8 entries, MRU-ordered).
- **Drag-and-drop or `ratbuddyssey foo.ady`** from the command line to open
  a calibration directly.
- **Dirty tracking**: title bar shows `Ratbuddyssey — file.ady*` while
  unsaved, with a confirm-discard prompt on close or open.

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

## File-association on Windows

Optional — associate `.ady` files with Ratbuddyssey so double-clicking opens
them. Edit the path in [`scripts/ratbuddyssey-ady-association.reg`](scripts/ratbuddyssey-ady-association.reg)
to point at your installed `Ratbuddyssey.exe`, then double-click the `.reg`
file. Remove with `scripts/ratbuddyssey-ady-association-remove.reg`.

## Acknowledgements

The hardware-quirks heuristics (receiver-model speed-of-sound list, ±12 dB
trim clamp, sub-trim floor, crossover snap list, default-curve replacement
rules used in the optional post-process) were derived from
[AudysseyOne](https://github.com/ObsessiveCompulsiveAudiophile/AudysseyOne)
by ObsessiveCompulsiveAudiophile, which is also MIT-licensed.

## License

[MIT](LICENSE).
