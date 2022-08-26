using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting.Internal;

namespace Atc.Daemon;

public static class PosixTerminationSignal
{
    public static async Task Receive()
    {
        var completion = new TaskCompletionSource();
        var handler = new SignalHandler(completion);
        await completion.Task;
    }

    private class SignalHandler
    {
        private readonly PosixSignalRegistration[] _registrations;
        private readonly TaskCompletionSource _completion;

        public SignalHandler(TaskCompletionSource completion)
        {
            _completion = completion;
            _registrations = new[] {
                PosixSignalRegistration.Create(PosixSignal.SIGINT, HandlePosixSignal),
                PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandlePosixSignal),
                PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandlePosixSignal)
            };
        }

        private void HandlePosixSignal(PosixSignalContext context)
        {
            Debug.Assert(context.Signal == PosixSignal.SIGINT || context.Signal == PosixSignal.SIGQUIT || context.Signal == PosixSignal.SIGTERM);
            context.Cancel = true;
            _completion.SetResult();
        }
    }
}
