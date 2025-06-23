using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Device.Gpio;

class Program
{
    private static DeviceClient _deviceClient;
    private static bool _currentState = false;
    private static bool _lastState = false;
    private static GpioController _ledPinController = new GpioController();
    private static GpioController _toggleSwitchPinController = new GpioController();
    private static int _toggleSwitchPinNumber = 14;
    private static int _ledPinNumber = 4;
    private static PinValue _ledPinValue;
    private static PinValue _toggleSwitchPinValue;
    
    private static string _deviceConnectionString =
        "HostName=Uni12TwinPro.azure-devices.net;DeviceId=TestDevice;SharedAccessKey=tYGn6+N1iCwjiGVaM8oJp3HzlLinx0W6w0bHoMw5HOo=";
    private static string _registryConnectionString =
        "HostName=Uni12TwinPro.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=ts51E0cBODPGFlLbDyoC7pZiHyzbP3wJ/AIoTODruRw=";
    static string _targetDeviceId = "TestDevice";
    
    static RegistryManager _registryManager;

    private static bool _ledState = false;
    static async Task Main(string[] args)
    {
        Task sendToggleTask = SendToggleStateAsync();
        Task recvLEDTask = RecvLEDStateAsync();
        try
        {
            _registryManager = RegistryManager.CreateFromConnectionString(_registryConnectionString);
            _ledPinController.OpenPin(_ledPinNumber, PinMode.Output);
            recvLEDTask.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"registry error: {ex.Message}");
        }
        
        try
        {
            _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString);
            _toggleSwitchPinController.OpenPin(_toggleSwitchPinNumber, PinMode.Input);
            sendToggleTask.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"device client error: {ex.Message}");
        }
        
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape)
                {
                    // TODO
                    // 두 Task 모두 종료 시키기
                    break;
                }
            }
        }
        
        Console.WriteLine($"program finish");
        if (_deviceClient != null)
        {
            await _deviceClient.CloseAsync();
        }

        if (_registryManager != null)
        {
            await _registryManager.CloseAsync();
        }
    }


    static async Task SendToggleStateAsync()
    {
        while (true)
        {
            Console.WriteLine($"[{DateTime.Now}] current state : {_currentState}");
            _toggleSwitchPinValue = _toggleSwitchPinController.Read(_toggleSwitchPinNumber);
            Console.WriteLine($"[{DateTime.Now}] pin value : {_toggleSwitchPinValue}");
            
            if (_toggleSwitchPinValue == PinValue.High)
            {
                _currentState = true;
            }
            else if (_toggleSwitchPinValue == PinValue.Low)
            {
                _currentState = false;
            }

            if (_currentState == _lastState)
            {
                return;
            }

            _lastState = _currentState;
            Console.WriteLine($"[{DateTime.Now}] current Toggle Switch state: {_currentState}");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["toggleState"] = _currentState;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            await Task.Delay(1000);
        }
    }

    static async Task RecvLEDStateAsync()
    {
        Console.WriteLine("RecvLEDStateAsync Start\n");
        while (true)
        {
            Twin twin = await _registryManager.GetTwinAsync(_targetDeviceId);
            ProcessTwinDataForLED(twin);
            
            await Task.Delay(1000);
        }
    }

    static void ProcessTwinDataForLED(Twin twin)
    {
        Console.WriteLine("ProcessTwinDataForLED Start\n");
        if (twin == null)
        {
            Console.WriteLine("Twin is null");
            return;
        }
        if (twin.Properties.Desired.Contains("ledState"))
        {
            Console.WriteLine("ledState Contain\n");
            object reported = twin.Properties.Desired["ledState"];
            if (reported != null)
            {
                PinValue ledValue = reported.ToString() == "True" ? PinValue.High : PinValue.Low; 
                Console.WriteLine($"current led state: {ledValue}");
                _ledPinController.Write(_ledPinNumber, ledValue);
            }
        }
    }
}