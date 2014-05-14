using System.Collections.Generic;
using System.ServiceModel;

namespace SystemTemperatureService
{
    [ServiceContract]
    interface ISystemTemperatureService
    {
        [OperationContract]
        List<Device> GetDeviceList();

        [OperationContract]
        void Shutdown();
    }
}
