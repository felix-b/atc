using Atc.Speech.AzurePlugin;

namespace Atc.Telemetry.Impl;

public static class AzureSpeechSynthesisPlugin_Telemetry 
{
    public class Noop : AzureSpeechSynthesisPlugin.IThisTelemetry
    {
    }
}