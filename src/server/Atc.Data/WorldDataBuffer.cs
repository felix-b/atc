using System.IO;
using FlatSharp;

namespace Atc.Data
{
    public class WorldDataBuffer
    {
        public WorldDataBuffer(WorldData data)
        {
            Data = data;
        }

        private WorldDataBuffer(Stream input)
        {
            using var memoryStream = new MemoryStream();
            input.CopyTo(memoryStream);

            var byteArray = memoryStream.ToArray();
            Data = WorldData.Serializer.Parse(byteArray);
        }

        public void WriteTo(Stream output)
        {
            int maxBytes = WorldData.Serializer.GetMaxSize(Data);
            byte[] byteArray = new byte[maxBytes];
            int bytesWritten = WorldData.Serializer.Write(byteArray, Data);
            output.Write(byteArray, 0, bytesWritten);
        }

        public WorldData Data { get; init; }

        public static WorldDataBuffer ReadFrom(Stream input)
        {
            return new WorldDataBuffer(input);
        }
    }
}