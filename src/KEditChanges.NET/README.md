# keditchanges-cli

Umbrella executable: **compile** (managed `kcompiler` / nwnnsscomp-style args), **ncsdecomp** (`NcsDecompCli`), and **info** (KEditChanges library placeholder).

```text
keditchanges-cli --help
keditchanges-cli --version
keditchanges-cli compile -c script.nss -o out.ncs -g 1
keditchanges-cli ncsdecomp -i in.ncs -o out.nss -g k1
```

Assembly name **`keditchanges-cli`** avoids NuGet/solution restore name clashes with the **KEditChanges** class library when both are in `KPatcher.sln`.

Published next to **KPatcher** when using **`PublishBundledCliTools`** (see `src/KPatcher.UI/KPatcher.UI.csproj`).
