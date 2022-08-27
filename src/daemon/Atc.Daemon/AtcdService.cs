using Atc.Maths;
using Atc.Server;
using Atc.Telemetry;
using Atc.World;
using Atc.World.Communications;
using AtcdProto;

namespace Atc.Daemon;

public class AtcdService
{
    private readonly IMyTelemetry _telemetry;
    private readonly RunLoop _runLoop;
    
    public AtcdService(IMyTelemetry telemetry, RunLoop runLoop)
    {
        _telemetry = telemetry;
        _runLoop = runLoop;
    }
    
    [PayloadCase(AtcdClientToServer.PayloadOneofCase.connect_request)]
    public void Connect(
        IDeferredConnectionContext<AtcdServerToClient> connection, 
        AtcdClientToServer envelope)
    {
        var replyEnvelope = new AtcdServerToClient() {
            connect_reply = new AtcdServerToClient.ConnectReply() {
                Success = true,
                Error = string.Empty
            }
        };

        connection.FireMessage(replyEnvelope);
    }
    
    [PayloadCase(AtcdClientToServer.PayloadOneofCase.disconnect_request)]
    public void Disconnect(
        IDeferredConnectionContext<AtcdServerToClient> connection, 
        AtcdClientToServer envelope)
    {
        connection.DisposeObserver(connection.Id.ToString());
        connection.RequestClose();
    }

    [PayloadCase(AtcdClientToServer.PayloadOneofCase.start_radio_monitor_request)]
    public void StartRadioMonitor(
        IDeferredConnectionContext<AtcdServerToClient> connection, 
        AtcdClientToServer envelope)
    {
        var request = envelope.start_radio_monitor_request;
        _telemetry.VerboseStartRadioMonitorRequested(
            connectionId: connection.Id,
            lat: request.LocationLat, 
            lon: request.LocationLon,
            frequencyKhz: request.FrequencyKhz);
        
        var silo = _runLoop.GetFirByIcao("LLLL").Silo;
        silo.PostAsyncAction(123, StartRadioMonitorOnFirThread);

        void StartRadioMonitorOnFirThread()
        {
            _telemetry.DebugStartingRadioMonitorOnFirThread(
                connectionId: connection.Id,
                lat: request.LocationLat, 
                lon: request.LocationLon,
                frequencyKhz: request.FrequencyKhz);
            
            if (connection.Session.TryGet<RadioStationSoundMonitor>(out var oldMonitor))
            {
                if (oldMonitor != null)
                {
                    oldMonitor.Dispose();
                    _telemetry.InfoRadioMonitorDisposed(connectionId: connection.Id);
                }
                connection.Session.Set<RadioStationSoundMonitor>(null);
            }

            var world = silo.Grains.GetAllGrainsOfType<IWorldGrain>().First();
            var position = GeoPoint.LatLon(request.LocationLat, request.LocationLon);
            var radioMedium = world.Get().TryFindRadioMedium(
                position,
                Altitude.Ground,
                Frequency.FromKhz(request.FrequencyKhz));

            if (radioMedium != null)
            {
                var groundStation = radioMedium.Value.Get().GroundStation;
                var groundStationCallsign = groundStation.Get().Callsign.Full; 
                var newMonitor = new RadioStationSoundMonitor(
                    radioStation: groundStation,
                    dependencies: silo.Dependencies);
                
                connection.Session.Set(newMonitor);
                connection.FireMessage(new AtcdServerToClient() {
                    start_radio_monitor_reply = new AtcdServerToClient.StartRadioMonitorReply() {
                        Success = true,
                        Error = string.Empty,
                        StationCallsign = groundStationCallsign
                    }
                });

                _telemetry.InfoRadioMonitorStarted(
                    connectionId: connection.Id,
                    frequencyKhz: request.FrequencyKhz,
                    stationCallsign: groundStationCallsign);
            }
            else
            {
                _telemetry.ErrorMonitorGroundStationNotFound(
                    connectionId: connection.Id,
                    lat: request.LocationLat, 
                    lon: request.LocationLon,
                    frequencyKhz: request.FrequencyKhz);

                connection.FireMessage(new AtcdServerToClient() {
                    start_radio_monitor_reply = new AtcdServerToClient.StartRadioMonitorReply() {
                        Success = false,
                        Error = "RadioMediumNotFound"
                    }
                });
            }

            connection.RequestFlush();
        }
    }

    [PayloadCase(AtcdClientToServer.PayloadOneofCase.stop_radio_monitor_request)]
    public void StopRadioMonitor(
        IDeferredConnectionContext<AtcdServerToClient> connection, 
        AtcdClientToServer envelope)
    {
        var silo = _runLoop.GetFirByIcao("LLLL").Silo;
        var currentMonitor = connection.Session.Has<RadioStationSoundMonitor>()
            ? connection.Session.Get<RadioStationSoundMonitor>()
            : null;

        if (currentMonitor != null)
        {
            connection.Session.Remove<RadioStationSoundMonitor>();
            currentMonitor.Dispose();
        }

        connection.FireMessage(new AtcdServerToClient() {
            stop_radio_monitor_reply = new AtcdServerToClient.StopRadioMonitorReply() {
                Success = true,
                Error = string.Empty
            }
        });
    }

    [TelemetryName("AtcdService")]
    public interface IMyTelemetry : ITelemetry
    {
        void VerboseStartRadioMonitorRequested(long connectionId, float lat, float lon, int frequencyKhz);
        void DebugStartingRadioMonitorOnFirThread(long connectionId, float lat, float lon, int frequencyKhz);
        void InfoRadioMonitorDisposed(long connectionId);
        void InfoRadioMonitorStarted(long connectionId, int frequencyKhz, string stationCallsign);
        void ErrorMonitorGroundStationNotFound(long connectionId, float lat, float lon, int frequencyKhz);
    }
}
