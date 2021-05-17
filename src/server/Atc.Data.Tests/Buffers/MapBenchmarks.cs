using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Atc.Data.Buffers;
using Atc.Data.Buffers.Impl;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests.Buffers
{
    [TestFixture]
    public class MapBenchmarks
    {
        [Test]
        public void Benchmarks()
        {
            TimeSpan tPopulateDicitonary, tPopulateIntMap, tLookupDictionary, tLookupIntMap;
            Stopwatch stopper;
            Dictionary<int, TestItem> dictionary;

            BufferPtr<TestContainer> containerPtr;
            using var stream = new MemoryStream();
            using (CreateContextScope(out var contextBefore))
            {
                containerPtr = contextBefore.AllocateRecord(new TestContainer {
                    M = contextBefore.AllocateIntMap<TestItem>(bucketCount: 2048)
                });

                dictionary = new Dictionary<int, TestItem>();                
                ref IntMap<TestItem> map = ref containerPtr.Get().M;

                stopper = Stopwatch.StartNew();
                PopulateDictionary(dictionary);
                stopper.Stop();
                tPopulateDicitonary = stopper.Elapsed;
                
                stopper = Stopwatch.StartNew();
                PopulateIntMap(ref map);
                stopper.Stop();
                tPopulateIntMap = stopper.Elapsed;
                
                contextBefore.WriteTo(stream);
            }

            stream.Position = 0;
            
            using (CreateContextScope(stream, out var contextAfter))
            {
                ref IntMap<TestItem> map = ref containerPtr.Get().M;

                stopper = Stopwatch.StartNew();
                LookupDictionary(dictionary);
                stopper.Stop();
                tLookupDictionary = stopper.Elapsed;
                
                stopper = Stopwatch.StartNew();
                LookupIntMap(ref map);
                stopper.Stop();
                tLookupIntMap = stopper.Elapsed;
            }
            
            Console.WriteLine($">>> POPULATE >> Dictionary[{tPopulateDicitonary}] IntMap[{tPopulateIntMap}]");            
            Console.WriteLine($">>> LOOKUP   >> Dictionary[{tLookupDictionary}] IntMap[{tLookupIntMap}]");            
        }

        private void PopulateDictionary(Dictionary<int, TestItem> dictionary)
        {
            for (int i = 0; i < 30000; i++)
            {
                dictionary[i] = new TestItem() {X = i};
            }
        }
        
        private void PopulateIntMap(ref IntMap<TestItem> map)
        {
            var context = BufferContext.Current;
            
            for (int i = 0; i < 30000; i++)
            {
                var itemPtr = context.AllocateRecord<TestItem>();
                itemPtr.Get().X = i;
                map.Set(i, itemPtr);
                
                if (i == 7000 || i == 8190 || i == 8191 || i == 8192 || i == 8193)
                {
                    HumanReadableTextDump.WriteToFile(BufferContext.Current, $"D:\\intmap-bench.dump-{i}.txt");                    
                }
            }
        }

        private void LookupDictionary(Dictionary<int, TestItem> dictionary)
        {
            for (int i = 0; i < 30000; i++)
            {
                var value = dictionary[i];
                if (Math.Abs(value.X - i) > 0.001)
                {
                    throw new Exception();
                }
            }
        }

        private void LookupIntMap(ref IntMap<TestItem> map)
        {
            for (int i = 0; i < 30000; i++)
            {
                ref TestItem value = ref map[i];
                if (Math.Abs(value.X - i) > 0.001)
                {
                    throw new Exception();
                }
            }
        }

        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                    .WithTypes<TestContainer, TestItem>()
                    .WithIntMap<TestItem>()
                .End(out contextTemp);

            context = (BufferContext)contextTemp;
            return scope;
        }

        private BufferContextScope CreateContextScope(Stream stream, out BufferContext context)
        {
            context = BufferContext.ReadFrom(stream);
            return new BufferContextScope(context);
        }
        
        public struct TestItem
        {
            public double X;
        }

        public struct TestContainer
        {
            public IntMap<TestItem> M;
        }
    }
}