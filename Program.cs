using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Device.Gpio;
using Iot.Device.DHTxx;
class Program
{
    private static RegistryManager _registryManager;
    private static DeviceClient _deviceClient;
    
    // GPIO States
    private static bool _currentDoorState = false;
    private static bool _currentWindowState = false;
    
    private static bool _lastDoorState = false;
    private static bool _lastWindowState = false;
    
    private static bool _ledState = false;
    
    // GPIO Pin Controller
    private static GpioController _gpioController = new GpioController();
    
    // Pin Numbers
    private static int _doorSwitchPinNumber = 14;
    private static int _windowSwitchPinNumber = 15;
    private static int _ledPinNumber = 4;
    private static int _dhtPinNumber = 26;
    
    // Connection String
    private static string _deviceConnectionString =
        "HostName=Uni12TwinPro.azure-devices.net;DeviceId=TestDevice;SharedAccessKey=tYGn6+N1iCwjiGVaM8oJp3HzlLinx0W6w0bHoMw5HOo=";
    private static string _registryConnectionString =
        "HostName=Uni12TwinPro.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=ts51E0cBODPGFlLbDyoC7pZiHyzbP3wJ/AIoTODruRw=";
    static string _targetDeviceId = "TestDevice";
    
    // DTH11
    private static Dht11 dht11;

    static async Task Main(string[] args)
    {
        // 1) CancellationTokenSource 생성
        CancellationTokenSource cts = new CancellationTokenSource();
        dht11 = new Dht11(_dhtPinNumber, _gpioController);
        
        // 2) 초기화
        try
        {
            _registryManager = RegistryManager.CreateFromConnectionString(_registryConnectionString);
            _gpioController.OpenPin(_ledPinNumber, PinMode.Output);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"registry error: {ex.Message}");
        }

        try
        {
            _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString);
            _gpioController.OpenPin(_doorSwitchPinNumber, PinMode.InputPullUp);
            _gpioController.OpenPin(_windowSwitchPinNumber, PinMode.InputPullUp);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"device client error: {ex.Message}");
        }

        // 3) 토큰을 넘겨 Task 시작
        Task recvLEDTask  = RecvLEDStateAsync(cts.Token);
        Task sendToggleTask = SendToggleStateAsync(cts.Token);

        // 4) Escape 누르면 Cancellation 요청
        Console.WriteLine("Press ESC to exit...");
        while (!cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
            {
                cts.Cancel();
            }
            await Task.Delay(100);  // CPU 과다 사용 방지
        }

        // 5) 두 Task가 종료될 때까지 대기
        try
        {
            await Task.WhenAll(recvLEDTask, sendToggleTask);
        }
        catch (OperationCanceledException)
        {
            // 정상적인 취소
        }

        // 6) 리소스 정리
        Console.WriteLine("Program finish");
        if (_deviceClient != null)     await _deviceClient.CloseAsync();
        if (_registryManager != null)  await _registryManager.CloseAsync();

        while (true)
        {
            await Task.Delay(2000);

            if (dht11.TryReadHumidity(out var humidity))
            {
                if (dht11.TryReadTemperature(out var temperature))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 온도: {temperature.DegreesCelsius:F1} ℃, 습도: {humidity:F1} %");
                    continue;
                }
            }
            Console.WriteLine("읽기 실패 — 다시 시도합니다.");
        }
    }


    static async Task SendToggleStateAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            PinValue doorState = _gpioController.Read(_doorSwitchPinNumber);
            Console.WriteLine($"[{DateTime.Now}] Door State : {doorState}");
            PinValue windowState = _gpioController.Read(_windowSwitchPinNumber);
            Console.WriteLine($"[{DateTime.Now}] Window State : {windowState}");
            
            _currentDoorState = (doorState == PinValue.High);
            
            if (_currentDoorState != _lastDoorState)
            {
                _lastDoorState = _currentDoorState;
                Console.WriteLine($"[{DateTime.Now}] Door toggled: {_currentDoorState}");
                TwinCollection reportedProperties = new TwinCollection { ["doorState"] = _currentDoorState };
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, token);
            }
            
            _currentWindowState = (windowState == PinValue.High);
            if (_currentWindowState != _lastWindowState)
            {
                _lastWindowState = _currentWindowState;
                Console.WriteLine($"[{DateTime.Now}] Window toggled : {_currentWindowState}");
                TwinCollection reportedProperties = new TwinCollection { ["windowState"] = _currentWindowState };
                await  _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, token);
            }

            await Task.Delay(1000, token);
        }

        token.ThrowIfCancellationRequested();
    }

    static async Task RecvLEDStateAsync(CancellationToken token)
    {
        Console.WriteLine("RecvLEDStateAsync Start");
        while (!token.IsCancellationRequested)
        {
            Twin twin = await _registryManager.GetTwinAsync(_targetDeviceId, token);
            ProcessTwinDataForLED(twin);
            await Task.Delay(1000, token);
        }

        token.ThrowIfCancellationRequested();
    }

    static void ProcessTwinDataForLED(Twin twin)
    {
        if (twin == null)
        {
            return;
        }
        if (twin.Properties.Desired.Contains("ledState"))
        {
            object reported = twin.Properties.Desired["ledState"];
            if (reported != null)
            {
                PinValue ledValue = reported.ToString() == "True" ? PinValue.High : PinValue.Low; 
                Console.WriteLine($"current led state: {ledValue}");
                _gpioController.Write(_ledPinNumber, ledValue);
            }
        }
    }
}