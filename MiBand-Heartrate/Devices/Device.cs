using System.ComponentModel;

namespace MiBand_Heartrate.Devices
{
    public enum DeviceStatus { OFFLINE, ONLINE_UNAUTH, ONLINE_AUTH }

    public enum DeviceModel
    {
        [Description("Hidden")]
        DUMMY,

        [Description("Mi Band 2/3")]
        MIBAND_2_3,

        [Description("Mi Band 4/5")]
        MIBAND_4
    }

    public abstract class Device : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        DeviceStatus _status = DeviceStatus.OFFLINE;
        public DeviceStatus Status
        {
            get => _status;
            internal set
            {
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
            }
        }

        public DeviceModel Model { get; internal set; }

        ushort _heartrate;
        public ushort Heartrate
        {
            get => _heartrate;
            internal set
            {
                _heartrate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Heartrate"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinHeartrate"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxHeartrate"));
            }
        }

        ushort _minHeartrate = 1000;
        public ushort MinHeartrate
        {
            get
            {
                if (_heartrate < _minHeartrate && _heartrate != 0)
                {
                    _minHeartrate = _heartrate;
                }
                return _minHeartrate;
            }
        }

        ushort _maxHeartrate;
        public ushort MaxHeartrate
        {
            get
            {
                if (_heartrate > _maxHeartrate)
                {
                    _maxHeartrate = _heartrate;
                }
                return _maxHeartrate;
            }
        }

        bool _heartrateMonitorStarted;
        public bool HeartrateMonitorStarted
        {
            get => _heartrateMonitorStarted;
            internal set
            {
                _heartrateMonitorStarted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeartrateMonitorStarted"));
            }
        }

        private byte _battery;
        public byte Battery
        {
            get => _battery;
            internal set
            {
                _battery = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Battery)));
            }
        }

        private bool _isCharging;
        public bool IsCharging
        {
            get => _isCharging;
            internal set
            {
                _isCharging = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCharging)));
            }
        }

        public abstract void Dispose();
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Authenticate();
        public abstract void StartHeartrateMonitor(bool continuous = false);
        public abstract void StopHeartrateMonitor();
    }
}