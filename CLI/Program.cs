using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using ZenStates.Core;
using ZenTimings.Common;
using ZenTimings.Export;
using ZenTimings.Plugin;

namespace ZenTimings.Cli
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                Options options = Options.Parse(args);
                if (options.Help)
                {
                    Console.WriteLine("ZenTimings.Export --export-json UNIQUE_PATH --exit\n" +
                                      "Uses existing installed drivers/devices. No GUI, installer or service-start changes.\n" +
                                      "Offline only: --self-test UNIQUE_PATH --exit");
                    return 0;
                }
                SnapshotFiles.CheckDestination(options.Path);
                // The offline route deliberately branches before identity,
                // service queries, CpuSingleton or hardware initialization.
                SnapshotObject snapshot = options.SelfTest ? SyntheticSnapshot() : Capture();
                SnapshotFiles.SaveNew(options.Path, snapshot);
                Console.WriteLine("{\"status\":\"complete\",\"mode\":\"" +
                                  (options.SelfTest ? "synthetic_offline_test" : "one_shot_cli") + "\"}");
                return 0;
            }
            catch (ArgumentException error)
            {
                Console.Error.WriteLine(error.Message);
                return 2;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
                return 1;
            }
        }

        private static SnapshotObject Capture()
        {
            GuardTargetIdentity();
            Cpu cpu = null;
            AsusWMI asus = null;
            try
            {
                cpu = CpuSingleton.Instance;
                if (cpu.info.family == Cpu.Family.UNSUPPORTED || !cpu.RyzenSmu.IsLoaded)
                    throw new InvalidOperationException("CPU/SMU initialization did not succeed.");
                if (cpu.io == null || !cpu.io.IsInpOutDriverOpen())
                    throw new InvalidOperationException("Existing inpoutx64 device could not be opened.");
                var memory = cpu.GetMemoryConfig();
                if (memory == null || memory.Timings == null || memory.Timings.Count == 0)
                    throw new InvalidOperationException("No memory timings were captured.");

                // Core startup already reads every module/channel's timings,
                // AOD, APOB, SPD init fields and PMICs, just as the GUI does.
                // Refresh the same live data without constructing any Window.
                if (cpu.powerTable == null)
                    throw new InvalidOperationException("Power table is unavailable.");
                var first = memory.Timings.First().Value;
                cpu.powerTable.ConfiguredClockSpeed = first.Frequency;
                cpu.powerTable.MemRatio = first.Ratio;
                var timer = Stopwatch.StartNew();
                SMU.Status status;
                do
                {
                    status = cpu.RefreshPowerTable();
                    if (status != SMU.Status.OK) Thread.Sleep(200);
                } while (status != SMU.Status.OK && timer.ElapsedMilliseconds < 100000);
                if (status != SMU.Status.OK)
                    throw new InvalidOperationException("Power table refresh failed: " + status);
                cpu.systemInfo?.UpdateSensors();
                memory.RefreshTelemetry(0);
                // ASUS WMI remains optional; this target is a Gigabyte board.
                asus = new AsusWMI();
                if (!asus.Init()) { asus.Dispose(); asus = null; }
                else asus.UpdateSensors();
                var source = new SnapshotSource
                {
                    Plugins = new List<IPlugin>(),
                    AsusSensors = asus?.sensors,
                    LastRefreshUtc = DateTime.UtcNow,
                    AutoRefreshActive = false,
                    SynchronousCapture = true,
                };
                return CoreOptionsScope.Run(false, () => SnapshotBuilder.Build(source, new SnapshotOptions
                {
                    Sections = SnapshotSections.All,
                    IncludeSerialNumbers = false,
                    IncludeLegend = true,
                })).Add("capture_mode", "one_shot_cli");
            }
            finally
            {
                asus?.Dispose();
                // Close only this process's upstream handles. Unlike the GUI
                // exit path, no DriverCleaner/AutoUninstallDriver is invoked.
                cpu?.Dispose();
            }
        }

        private static void GuardTargetIdentity()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                    throw new InvalidOperationException("Run elevated; this command does not display UAC or install drivers.");
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                if (!(key?.GetValue("ProcessorNameString") as string ?? "").Contains("9950X3D"))
                    throw new InvalidOperationException("Only the authorized 9950X3D target may perform live CLI capture.");
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
                if ((key?.GetValue("BaseBoardProduct") as string) != "B850M AORUS ELITE WIFI6E ICE")
                    throw new InvalidOperationException("Target motherboard mismatch.");
        }

        private static SnapshotObject SyntheticSnapshot()
        {
            var root = new SnapshotObject().Add("schema", SnapshotBuilder.SchemaName)
                .Add("schema_version", SnapshotBuilder.SchemaVersion)
                .Add("generated_at", DateTime.UtcNow.ToString("o"))
                .Add("capture_mode", "synthetic_offline_test");
            var statics = new SnapshotObject();
            foreach (string name in new[] { "system", "memory", "spd" })
                statics.Add(name, new SnapshotObject().Add("synthetic", true));
            var config = new SnapshotObject();
            foreach (string name in new[] { "timings", "aod", "apob" })
                config.Add(name, new SnapshotObject().Add("synthetic", true));
            var readings = new SnapshotObject();
            foreach (string name in new[] { "power_table", "sensors", "dimm" })
                readings.Add(name, new SnapshotObject().Add("synthetic", true));
            return root.Add("static", statics).Add("config", config).Add("readings", readings);
        }
    }

    internal sealed class Options
    {
        internal string Path;
        internal bool SelfTest;
        internal bool Help;

        internal static Options Parse(string[] args)
        {
            if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h")) return new Options { Help = true };
            if (args.Length != 3 || args[2] != "--exit" ||
                (args[0] != "--export-json" && args[0] != "--self-test") || string.IsNullOrWhiteSpace(args[1]))
                throw new ArgumentException("Usage: ZenTimings.Export --export-json UNIQUE_PATH --exit");
            return new Options { Path = System.IO.Path.GetFullPath(args[1]), SelfTest = args[0] == "--self-test" };
        }
    }

    internal static class SnapshotFiles
    {
        internal static void CheckDestination(string path)
        {
            if (File.Exists(path)) throw new IOException("Destination already exists; refusing overwrite.");
            string parent = Path.GetDirectoryName(path);
            if (!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
        }

        internal static void Validate(SnapshotObject snapshot)
        {
            foreach (var group in new[] {
                new[] { "static", "system", "memory", "spd" },
                new[] { "config", "timings", "aod", "apob" },
                new[] { "readings", "power_table", "sensors", "dimm" } })
            {
                if (!snapshot.TryGet(group[0], out object value) || !(value is SnapshotObject obj))
                    throw new InvalidDataException("Missing snapshot group: " + group[0]);
                foreach (string section in group.Skip(1))
                    if (!obj.TryGet(section, out object data) || data == null)
                        throw new InvalidDataException("Missing snapshot section: " + group[0] + "." + section);
            }
        }

        internal static void SaveNew(string path, SnapshotObject snapshot)
        {
            Validate(snapshot);
            string json = SnapshotWriter.Write(snapshot, SnapshotFormat.Json);
            string temporary = path + ".partial." + Guid.NewGuid().ToString("N");
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(file, new UTF8Encoding(false)))
                    writer.Write(json);
                File.Move(temporary, path); // no replacement of an existing export
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
