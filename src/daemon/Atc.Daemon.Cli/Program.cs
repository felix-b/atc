// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using Atc.Server;
using AtcdProto;

int cliExitCode = 0;

Console.WriteLine("Welcome to atcli, a CLI for Air Traffic & Control simulation daemon (atcd)");

var rootCommand = new RootCommand();
var monitorCommand = new Command("monitor", "Monitor radio stations");
rootCommand.Add(monitorCommand);
var monitorStartCommand = new Command("start", "Start monitoring radio station");
monitorCommand.Add(monitorStartCommand);

var latitudeOption = new Option<double>(name: "--lat", description: "Latitude of the radio receiver");
latitudeOption.IsRequired = true;

var longitudeOption = new Option<double>(name: "--lon", description: "Longitude of the radio receiver");
longitudeOption.IsRequired = true;

var frequencyKhzOption = new Option<int>(name: "--khz", description: "Frequency to tune in KHz");
frequencyKhzOption.IsRequired = true;

monitorStartCommand.Add(latitudeOption);
monitorStartCommand.Add(longitudeOption);
monitorStartCommand.Add(frequencyKhzOption);

monitorStartCommand.SetHandler(
    async (lat, lon, khz) => {
        Console.WriteLine($"Starting radio monitor: lat[{lat}] lon[{lon}] khz[{khz}]");
        try
        {
            await using (var client = CreateServiceClient())
            {
                await client.SendEnvelope(new AtcdClientToServer() {
                    start_radio_monitor_request = new AtcdClientToServer.StartRadioMonitorRequest() {
                        LocationLat = (float)lat,
                        LocationLon = (float)lon,
                        FrequencyKhz = khz
                    }
                });

                var replyEnvelope = await client.WaitForIncomingEnvelope(e => e.start_radio_monitor_reply != null, 10000);
                if (replyEnvelope == null)
                {
                    cliExitCode = 100;
                    Console.Error.WriteLine("Send request to daemon, but timed out waiting for reply");
                }
                else if (!replyEnvelope.start_radio_monitor_reply.Success)
                {
                    cliExitCode = 10;
                    Console.WriteLine($"Could not start radio monitor: error={replyEnvelope.start_radio_monitor_reply.Error}");
                }
                else
                {
                    Console.WriteLine($"Radio monitor started.");
                }
            }
        }
        catch (Exception e)
        {
            cliExitCode = 200;
            Console.Error.WriteLine("ERROR! " + e.ToString());
        }
    },
    latitudeOption, 
    longitudeOption, 
    frequencyKhzOption
);

await rootCommand.InvokeAsync(args);
return cliExitCode;


static WebSocketServiceClient<AtcdClientToServer, AtcdServerToClient> CreateServiceClient()
{
    return new WebSocketServiceClient<AtcdClientToServer, AtcdServerToClient>("ws://localhost:3001/atc");
}

