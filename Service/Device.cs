using System.Runtime.Serialization;

namespace SystemTemperatureService
{
    [DataContract]
    public enum DeviceType
    {
        [EnumMember]
        Cpu,

        [EnumMember]
        Gpu,

        [EnumMember]
        Hdd
    }

    [DataContract]
    public class Device
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public DeviceType Type { get; set; }

        [DataMember]
        public double Temperature { get; set; }
    }
}
