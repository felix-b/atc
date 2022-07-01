using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AtcServerSamplesProto;

namespace Atc.Server.Tests.Samples;

public class SampleServiceOne
{
    private readonly ConcurrentDictionary<string, int> _counterValueByName = new();

    [PayloadCase(Sample1ClientToServer.PayloadOneofCase.hello_request)]
    public void Hello(
        IDeferredConnectionContext<Sample1ServerToClient> connection, 
        Sample1ClientToServer envelope)
    {
        var request = envelope.hello_request;

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new Exception("Request validation failed");
        }
        
        _counterValueByName[request.Name] = request.InitialCounterValue;

        connection.Session.Set(new SessionInfo(request.Name));
        connection.FireMessage(new Sample1ServerToClient {
            greeting_reply = new Sample1ServerToClient.GreetingReply {
                Greeting = $"Hello {request.Name}"
            }
        });
    }

    [PayloadCase(Sample1ClientToServer.PayloadOneofCase.query_counter_request)]
    public void QueryCounterRequest(
        IDeferredConnectionContext<Sample1ServerToClient> connection, 
        Sample1ClientToServer envelope)
    {
        var info = 
            connection.Session.Get<SessionInfo>() 
            ?? throw new Exception("Session info not found");

        connection.FireMessage(new Sample1ServerToClient {
            query_counter_reply = new Sample1ServerToClient.QueryCounterReply {
                CounterValue = _counterValueByName[info.Name]
            }
        });
    }

    [PayloadCase(Sample1ClientToServer.PayloadOneofCase.goodbye_request)]
    public void Goodbye(
        IDeferredConnectionContext<Sample1ServerToClient> connection, 
        Sample1ClientToServer envelope)
    {
        var info = 
            connection.Session.Get<SessionInfo>() 
            ?? throw new Exception("Session info not found");

        _counterValueByName.Remove(info.Name, out _);
        connection.RequestClose();
    }

    public record SessionInfo(string Name);
}
