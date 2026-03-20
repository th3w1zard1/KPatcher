# One-off: emit PatcherResources.Designer.cs property lines
import re
from pathlib import Path
resx = Path(__file__).resolve().parents[1] / "src" / "KPatcher.Core" / "Resources" / "PatcherResources.resx"
content = resx.read_text(encoding="utf-8")
keys = sorted(set(re.findall(r'<data name="([^"]+)"', content)))
for k in keys:
    print(f"        public static string {k} => ResourceManager.GetString(nameof({k}), _resourceCulture);")
