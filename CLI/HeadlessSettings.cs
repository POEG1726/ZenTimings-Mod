// Metadata adapters only. The full GUI settings loader can prompt/save settings;
// the CLI always requests every snapshot section and does not use hidden sensors.
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZenTimings.Settings
{
    internal sealed class AppSettings
    {
        internal static AppSettings Instance { get; } = new AppSettings();
        public bool AdvancedMode => true;
        public int AutoRefreshInterval => 0;
    }

    internal sealed class SensorSettings
    {
        internal static SensorSettings Instance { get; } = new SensorSettings();
        public List<string> HiddenSensors { get; } = new List<string>();
    }
}

namespace ZenTimings.Helpers
{
    internal static class DriverHelper
    {
        public static System.Version Version => ZenStates.Core.PawnIo.PawnIo.Version;
    }
}

namespace ZenTimings.Common
{
    // ASUS sensor models need INotifyPropertyChanged but no WPF dispatcher.
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
