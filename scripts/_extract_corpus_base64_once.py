"""One-off: read Base64 from corpus fixture .cs files and write binary/text under test_files."""
import base64
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "tests/KPatcher.Tests/test_files/integration_tslpatcher_archive_corpus"

# (source_cs_glob_relative, [(const_name, out_relative_path, mode)])
# mode: "bytes" | "utf8_from_b64" (alpha changes.ini)
JOBS = [
    (
        "tests/KPatcher.Tests/Integration/EmbeddedTslpatcherArchiveCorpus/K1ArchiveCorpusCaseDeltaFixture.cs",
        [
            ("TslpatchdataVisualeffects2daBase64", "k1_case_delta/visualeffects.2da", "bytes"),
            ("TslpatchdataInfoRtfRawBase64", "k1_case_delta/info.rtf", "bytes"),
        ],
    ),
    (
        "tests/KPatcher.Tests/Integration/EmbeddedTslpatcherArchiveCorpus/TslArchiveCorpusCaseEpsilonFixture.cs",
        [
            ("TslpatchdataVisualeffects2daBase64", "tsl_case_epsilon/visualeffects.2da", "bytes"),
            ("TslpatchdataInfoRtfRawBase64", "tsl_case_epsilon/info.rtf", "bytes"),
        ],
    ),
    (
        "tests/KPatcher.Tests/Integration/EmbeddedTslpatcherArchiveCorpus/TslArchiveCorpusCaseAlphaFixture.cs",
        [
            ("TslpatchdataChangesIniUtf8Base64", "tsl_case_alpha/tslpatchdata_changes.ini.txt", "utf8_from_b64"),
            ("TslpatchdataAStockstore01NcsBase64", "tsl_case_alpha/a_stockstore_01.ncs", "bytes"),
            ("TslpatchdataInfoRtfRawBase64", "tsl_case_alpha/info.rtf", "bytes"),
        ],
    ),
    (
        "tests/KPatcher.Tests/Integration/EmbeddedTslpatcherArchiveCorpus/TslArchiveCorpusCaseBetaFixture.cs",
        [
            ("ReadMeTxtRawBase64", "tsl_case_beta/readme.txt", "bytes"),
            ("InfoRtfRawBase64", "tsl_case_beta/info.rtf", "bytes"),
            ("ATransformT3M4NcsBase64", "tsl_case_beta/a_transformt3m4.ncs", "bytes"),
            ("LlkProitemsUtpBase64", "tsl_case_beta/llk_proitems.utp", "bytes"),
        ],
    ),
    (
        "tests/KPatcher.Tests/Integration/EmbeddedTslpatcherArchiveCorpus/K1ArchiveCorpusCaseGammaFixture.cs",
        [
            ("TslpatchdataInfoRtfRawBase64", "k1_case_gamma/tslpatchdata_info.rtf", "bytes"),
            ("WmotrInfoWmotrRtfRawBase64", "k1_case_gamma/wmotr_info_wmotr.rtf", "bytes"),
            ("TslpatchdataK37ItmAjuntaUtiBase64", "k1_case_gamma/tsl_k37_itm_ajunta.uti", "bytes"),
            ("TslpatchdataK37ItmFreednf1UtiBase64", "k1_case_gamma/tsl_k37_itm_freednf1.uti", "bytes"),
            ("TslpatchdataK37ItmFreednf2UtiBase64", "k1_case_gamma/tsl_k37_itm_freednf2.uti", "bytes"),
            ("TslpatchdataK37ItmFreedontUtiBase64", "k1_case_gamma/tsl_k37_itm_freedont.uti", "bytes"),
            # ajunta / freednf1 / freednf2: WMOTR bytes matched TSL; corpus keeps TSL paths only.
            ("WmotrK37ItmFreedontUtiBase64", "k1_case_gamma/wm_k37_itm_freedont.uti", "bytes"),
        ],
    ),
]


def extract_const(text: str, name: str) -> str:
    # Prefer verbatim @"..."; fall back to regular "..." (single line)
    m = re.search(
        rf"internal const string {re.escape(name)}\s*=\s*@\"(.*?)\"\s*;",
        text,
        re.DOTALL,
    )
    if m:
        return m.group(1)
    m2 = re.search(
        rf"internal const string {re.escape(name)}\s*=\s*\"([^\"]*)\"\s*;",
        text,
        re.DOTALL,
    )
    if m2:
        return m2.group(1)
    raise SystemExit(f"missing {name}")


def main():
    for rel_cs, items in JOBS:
        p = ROOT / rel_cs
        text = p.read_text(encoding="utf-8")
        for const_name, out_rel, mode in items:
            raw = extract_const(text, const_name)
            out = OUT / out_rel
            out.parent.mkdir(parents=True, exist_ok=True)
            if mode == "utf8_from_b64":
                data = base64.b64decode(raw)
                out.write_bytes(data)
            else:
                data = base64.b64decode(raw)
                out.write_bytes(data)
            print(out_rel, len(data))


if __name__ == "__main__":
    main()
