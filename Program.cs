using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Device.Gpio;

class Program
{
    private static DeviceClient _deviceClient;
    private static bool _currentState = false;
    private static bool _lastState = false;
    private static GpioController _gpioController = new GpioController();
    private static int _toggleSwitchPinNumber = 14;
    private static int _ledPinNumber = 4;
    
    private static string _deviceConnectionString =
        "HostName=Uni12TwinPro.azure-devices.net;DeviceId=TestDevice;SharedAccessKey=tYGn6+N1iCwjiGVaM8oJp3HzlLinx0W6w0bHoMw5HOo=";
    private static string _registryConnectionString =
        "HostName=Uni12TwinPro.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=ts51E0cBODPGFlLbDyoC7pZiHyzbP3wJ/AIoTODruRw=";
    static string _targetDeviceId = "TestDevice";
    
    private static RegistryManager _registryManager;

    private static bool _ledState = false;
    static async Task Main(string[] args)
    {
        // 1) CancellationTokenSource 생성
        CancellationTokenSource cts = new CancellationTokenSource();

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
            _gpioController.OpenPin(_toggleSwitchPinNumber, PinMode.Input);
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
    }


    static async Task SendToggleStateAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Console.WriteLine($"[{DateTime.Now}] current state : {_currentState}");
            PinValue readValue = _gpioController.Read(_toggleSwitchPinNumber);
            Console.WriteLine($"[{DateTime.Now}] pin value : {readValue}");
            
            _currentState = (readValue == PinValue.High);

            if (_currentState != _lastState)
            {
                _lastState = _currentState;
                Console.WriteLine($"[{DateTime.Now}] toggled: {_currentState}");
                TwinCollection reportedProperties = new TwinCollection { ["toggleState"] = _currentState };
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, token);
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
        Console.WriteLine("ProcessTwinDataForLED Start");
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