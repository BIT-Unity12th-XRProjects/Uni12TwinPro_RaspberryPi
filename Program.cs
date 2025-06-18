using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Device.Gpio;

class Program
{
    static DeviceClient? deviceClient;
    static bool currentState = false;

    static async Task Main(string[] args)
    {
        try
        {
            var connectionString = "HostName=Uni12TwinProTest.azure-devices.net;DeviceId=azure-samples-test;SharedAccessKey=4GPoU+qynj2+RZnmzlFti2U5yUEJ7nRL/KvJpiUtsno=";
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            Console.WriteLine("스페이스바 토글 / ESC 종료");

            bool running = true;
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Spacebar)
                    {
                        await ToggleStateAsync();
                        await Task.Delay(300);
                    }
                    else if (key == ConsoleKey.Escape)
                    {
                        running = false;
                    }
                }


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

    static async Task ToggleStateAsync()
    {
        currentState = !currentState;
        var reportedProperties = new TwinCollection();
        reportedProperties["buttonState"] = currentState;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        Console.WriteLine($"[{DateTime.Now}] 상태 업데이트: {currentState}");
    }
}