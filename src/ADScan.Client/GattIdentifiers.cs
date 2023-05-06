using System;

namespace ADScan.Client
{
    public class GattIdentifiers
    {
        //public static Guid UartGattServiceId = Guid.Parse("00001800-0000-1000-8000-00805f9b34fb");
        public static Guid UartGattServiceId = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
        //public static Guid UartGattCharacteristicReceiveId = Guid.Parse("00002a00-0000-1000-8000-00805f9b34fb");
        public static Guid UartGattCharacteristicReceiveId = Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
        //public static Guid UartGattCharacteristicSendId = Guid.Parse("00002a00-0000-1000-8000-00805f9b34fb");
        public static Guid UartGattCharacteristicSendId = Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        public static Guid SpecialNotificationDescriptorId = Guid.Parse("00002902-0000-1000-8000-00805f9b34fb");
    }
}