#if false
using Atc.Speech.AzurePlugin;
using Atc.Telemetry;
using Atc.Telemetry.CodePath;

namespace GeneratedCode;

public static class AtcSpeechAzurePluginTelemetry
{
    public static ITelemetryImplementationMap GetCodePathImplementations(CodePathEnvironment environment)
    {
        return new CodePathImplementationMap(environment);
    }
    
    public static ITelemetryImplementationMap GetNoopImplementations()
    {
        return new NoopImplementationMap();
    }
    
    public static ITelemetryImplementationMap GetTestDoubleImplementations()
    {
        return new TestDoubleImplementationMap();
    }

    public static T CreateNoopTelemetry<T>() where T : ITelemetry
    {
        var entry = new NoopImplementationMap().GetEntries().First(e => e.InterfaceType == typeof(T));
        return (T)entry.Factory();
    }

    public static T CreateTestDoubleTelemetry<T>() where T : ITelemetry
    {
        var entry = new TestDoubleImplementationMap().GetEntries().First(e => e.InterfaceType == typeof(T));
        return (T)entry.Factory();
    }

    private class CodePathImplementationMap : ITelemetryImplementationMap
    {
        private readonly CodePathEnvironment _environment; 
        
        public CodePathImplementationMap(CodePathEnvironment environment)
        {
            _environment = environment;
        }
        
        public TelemetryImplementationEntry[] GetEntries()
        {
            return new[] {
                new TelemetryImplementationEntry(
                    typeof(AzureSpeechSynthesisPlugin.IMyTelemetry),
                    () => new CodePathImpl__Atc_Speech_AzurePlugin_AzureSpeechSynthesisPlugin_IMyTelemetry(new CodePathWriter(_environment, "AzureSpeechSynthesisPlugin"))
                )
            };
        }
    }

    private class NoopImplementationMap : ITelemetryImplementationMap
    {
        public TelemetryImplementationEntry[] GetEntries()
        {
            return new[] {
                new TelemetryImplementationEntry(
                    typeof(AzureSpeechSynthesisPlugin.IMyTelemetry),
                    () => new NoopImpl__Atc_Speech_AzurePlugin_AzureSpeechSynthesisPlugin_IMyTelemetry()
                )
            };
        }
    }

    private class TestDoubleImplementationMap : ITelemetryImplementationMap
    {
        public TelemetryImplementationEntry[] GetEntries()
        {
            return new[] {
                new TelemetryImplementationEntry(
                    typeof(AzureSpeechSynthesisPlugin.IMyTelemetry),
                    () => new TestDoubleImpl__Atc_Speech_AzurePlugin_AzureSpeechSynthesisPlugin_IMyTelemetry()
                )
            };
        }
    }
}
#endif