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
    
    static string connectionSendString = "HostName=Uni12TwinProTest.azure-devices.net;DeviceId=azure-samples-test;SharedAccessKey=4GPoU+qynj2+RZnmzlFti2U5yUEJ7nRL/KvJpiUtsno=";
    // static string connectionRecvString = "HostName=Uni12TwinProTest.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=Ez/vrnK5xfmYrosJCMRRkR7wuIDZGSV/BAIoTNvpoiU=";
    
    // static RegistryManager registryManager;
    static async Task Main(string[] args)
    {
        // registryManager = RegistryManager.CreateFromConnectionString(connectionRecvString);
        
        try
        {
            deviceClient = DeviceClient.CreateFromConnectionString(connectionSendString);

            gpio.OpenPin(toggleSwitchPin, PinMode.Input);
            
            bool running = true;
            while (running)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape)
                {
                    running = false;
                }

                PinValue value = gpio.Read(toggleSwitchPin);
                await SendToggleStateAsync(value);
                await Task.Delay(50);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"에러 발생: {ex.Message}");
        }
        finally
        {
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

    static async Task SendToggleStateAsync(PinValue value)
    {
        if (value == PinValue.High)
        {
            currentState = true;
        }
        else if (value == PinValue.Low)
        {
            currentState = false;
        }
        
        if (currentState == lastState)
        {
            return;
        }
        
        lastState = currentState;
        TwinCollection reportedProperties = new TwinCollection();
        reportedProperties["toggleState"] = currentState;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        Console.WriteLine($"[{DateTime.Now}] 상태 업데이트: {currentState}");
    }
}