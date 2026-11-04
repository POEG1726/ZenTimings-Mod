"""Exercise only explicit synthetic/argument/preflight-file paths, never hardware."""
import concurrent.futures
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / "artifacts" / "headless" / "ZenTimings.Export.exe"


def invoke(*args):
    return subprocess.run([str(EXE), *map(str, args)], capture_output=True, encoding="utf-8", errors="replace", timeout=20)


class HeadlessExportTests(unittest.TestCase):
    def test_synthetic_full_json_uses_shared_writer(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "offline.json"
            result = invoke("--self-test", path, "--exit")
            self.assertEqual(result.returncode, 0, result.stderr)
            snapshot = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(snapshot["schema"], "zentimings.snapshot")
            self.assertEqual(snapshot["capture_mode"], "synthetic_offline_test")
            for group, sections in {"static": ("system", "memory", "spd"),
                                    "config": ("timings", "aod", "apob"),
                                    "readings": ("power_table", "sensors", "dimm")}.items():
                for section in sections:
                    self.assertTrue(snapshot[group][section]["synthetic"])

    def test_existing_destination_rejected_before_live_capture(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "already.json"
            path.write_bytes(b"immutable-original")
            # CheckDestination fails before the live Capture method is called.
            result = invoke("--export-json", path, "--exit")
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("refusing overwrite", result.stderr)
            self.assertEqual(path.read_bytes(), b"immutable-original")

    def test_invalid_arguments_stop_before_capture(self):
        result = invoke("--export-json")
        self.assertEqual(result.returncode, 2)
        self.assertIn("Usage:", result.stderr)

    def test_help_needs_no_driver(self):
        result = invoke("--help")
        self.assertEqual(result.returncode, 0)
        self.assertIn("--export-json", result.stdout)

    def test_two_synthetic_writers_never_replace_one_another(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "unique.json"
            with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
                results = list(pool.map(lambda _: invoke("--self-test", path, "--exit"), range(2)))
            self.assertEqual(sorted(x.returncode for x in results), [0, 1])
            self.assertEqual(json.loads(path.read_text())["capture_mode"], "synthetic_offline_test")
            self.assertEqual(list(Path(directory).glob("*.partial.*")), [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
