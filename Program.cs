using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Device.Gpio;

class Program
{
    static DeviceClient? deviceClient;
    static bool currentState = false;
    private static bool lastState = false;
    private static GpioController gpio = new GpioController();
    private static int toggleSwitchPin = 4;
    private static int ledPin = 4;
    private static PinValue pinValue;
    
    static string connectionSendString = "HostName=Uni12TwinPro.azure-devices.net;DeviceId=TestDevice;SharedAccessKey=tYGn6+N1iCwjiGVaM8oJp3HzlLinx0W6w0bHoMw5HOo=";
    static string connectionRecvString = "HostName=Uni12TwinProTest.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=Ez/vrnK5xfmYrosJCMRRkR7wuIDZGSV/BAIoTNvpoiU=";
    static string targetDeviceId = "TestDevice";
    static RegistryManager registryManager;

    private static bool _ledState = false;
    static async Task Main(string[] args)
    {
        try
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionRecvString);
            // deviceClient = DeviceClient.CreateFromConnectionString(connectionSendString);

            // gpio.OpenPin(toggleSwitchPin, PinMode.Input);
            gpio.OpenPin(ledPin, PinMode.Output);
            
            // Console.WriteLine($"[{DateTime.Now}] current state : {currentState}");
            // pinValue = gpio.Read(toggleSwitchPin);
            // Console.WriteLine($"[{DateTime.Now}] pin value : {pinValue}");

            bool running = true;
            while (running)
            {
                // pinValue = gpio.Read(toggleSwitchPin);
                //
                // await SendToggleStateAsync();
                // await Task.Delay(50);

                await RecvLEDStateAsync();
                await Task.Delay(50);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                    {
                        running = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"에러 발생: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"program finish");
            if (deviceClient != null)
            {
                await deviceClient.CloseAsync();
            }
        }
    }

    // static async Task RecvToggleStateAsync()
    // {
    //     TwinCollection recvState = new TwinCollection();
    //     
    // }

    // static async Task SendToggleStateAsync()
    // {
    //     //Console.WriteLine($"[{DateTime.Now}] SendToggleStateAsync");
    //
    //     if (pinValue == PinValue.High)
    //     {
    //         currentState = true;
    //     }
    //     else if (pinValue == PinValue.Low)
    //     {
    //         currentState = false;
    //     }
    //
    //     if (currentState == lastState)
    //     {
    //         return;
    //     }
    //
    //     lastState = currentState;
    //     Console.WriteLine($"[{DateTime.Now}] current state: {currentState}");
    //     TwinCollection reportedProperties = new TwinCollection();
    //     reportedProperties["toggleState"] = currentState;
    //
    //     await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    // }

    static async Task RecvLEDStateAsync()
    {
        Console.WriteLine("RecvLEDStateAsync Start\n");
        while (true)
        {
            // Task<Twin> twinTask = registryManager.GetTwinAsync(targetDeviceId);
            
            Twin twin = await registryManager.GetTwinAsync(targetDeviceId);
            ProcessTwinDataForLED(twin);
            await Task.Delay(1000);
        }
    }

    static void ProcessTwinDataForLED(Twin twin)
    {
        if (twin.Properties.Reported.Contains("ledState"))
        {
            object reported = twin.Properties.Reported["ledState"];
            if (reported != null)
            {
                PinValue ledValue = reported.ToString() == "True" ? PinValue.High : PinValue.Low; 
                Console.WriteLine($"current led state: {ledValue}");
                gpio.Write(ledPin, ledValue);
            }
        }
    }
}