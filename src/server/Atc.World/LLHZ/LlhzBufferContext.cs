using System;
using Atc.Data;
using Atc.Data.Sources;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;

namespace Atc.World.LLHZ
{
    public class LlhzBufferContext : IDisposable
    {
        private IBufferContext? _bufferContext = null;
        private IDisposable? _bufferScope = null;

        public LlhzBufferContext()
        {
            BufferContextScope.UseStaticScope();
            _bufferScope = AtcBufferContext.CreateEmpty(out _bufferContext);
            LoadTypes();
            LoadTailNumbers();
        }
        
        public void Dispose()
        {
            BufferContextScope.ClearStaticScope();
            _bufferContext = null;
            _bufferScope = null;
        }

        public IBufferContext BufferContext => _bufferContext;
        
        private void LoadTypes()
        {
            var context = _bufferContext!;
            ref var worldData = ref context.GetWorldData();

            var dummyFlightModelRef = context.AllocateRecord(new FlightModelData());

            worldData.TypeByIcao.Add(
                context.AllocateString("C172"),
                context.AllocateRecord(new AircraftTypeData() {
                    Icao = context.AllocateString("C172"),
                    Name = context.AllocateString("Cessna 172"),
                    Callsign = context.AllocateString("Cessna"),
                    Category = AircraftCategories.Prop,
                    Operations = OperationTypes.GA,
                    FlightModel = dummyFlightModelRef,
                })
            );

            worldData.TypeByIcao.Add(
                context.AllocateString("C152"),
                context.AllocateRecord(new AircraftTypeData() {
                    Icao = context.AllocateString("C152"),
                    Name = context.AllocateString("Cessna 152"),
                    Callsign = context.AllocateString("Cessna"),
                    Category = AircraftCategories.Prop,
                    Operations = OperationTypes.GA,
                    FlightModel = dummyFlightModelRef,
                })
            );
        }

        private void LoadTailNumbers()
        {
            var context = _bufferContext!;

            for (int i = 0; i < LlhzFacts.AircraftList.Count; i++)
            {
                context.AllocateString(LlhzFacts.AircraftList[i].TailNo);
            }
        }
    }
}