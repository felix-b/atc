using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Atc.Data.Buffers;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests.Buffers
{
    [TestFixture]
    public unsafe class TypedBufferTests
    {
        [Test]
        public void InitiallyEmpty()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);

            context.Buffer.ReadOnly.Should().BeFalse();
            context.Buffer.InitialCapacity.Should().Be(10);
            context.Buffer.RecordCount.Should().Be(0);
            context.Buffer.TotalBytes.Should().Be(10 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(0);
        }

        [Test]
        public void AllocateSingleRecord_StateUpdated()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            
            context.Buffer.Allocate();
            
            context.Buffer.InitialCapacity.Should().Be(10);
            context.Buffer.RecordCount.Should().Be(1);
            context.Buffer.TotalBytes.Should().Be(10 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(sizeof(ASimpleRecord));
        }

        [Test]
        public void BufferOverflow_ReallocatedToLargerBuffer()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 3);
            context.Buffer.Allocate();
            context.Buffer.Allocate();
            context.Buffer.Allocate();
            
            context.Buffer.InitialCapacity.Should().Be(3);
            context.Buffer.RecordCount.Should().Be(3);
            context.Buffer.TotalBytes.Should().Be(3 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(3 * sizeof(ASimpleRecord));
            
            context.Buffer.Allocate();

            context.Buffer.InitialCapacity.Should().Be(3);
            context.Buffer.RecordCount.Should().Be(4);
            context.Buffer.TotalBytes.Should().Be(6 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(4 * sizeof(ASimpleRecord));
        }

        [Test]
        public void AllocateSingleRecord_ValuesZeroed()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            
            var ptr = context.Buffer.Allocate();
            
            ref ASimpleRecord record = ref ptr.Get();
            record.N.Should().Be(0);
            record.B.Should().Be(false);
            record.X.Should().Be(0);
        }

        [Test]
        public void AllocateSingleRecord_CopyInitialValues()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            
            var ptr = context.Buffer.Allocate(new ASimpleRecord {
                N = 123,
                B = true, 
                X = 44.4
            });
            
            ref ASimpleRecord record = ref ptr.Get();
            record.N.Should().Be(123);
            record.B.Should().Be(true);
            record.X.Should().Be(44.4);
        }

        [Test]
        public void ScalarsSurviveBufferRoundtrip()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            var ptr = context.Buffer.Allocate();

            ptr.Get().N = 123;
            ptr.Get().B = true;
            ptr.Get().X = 456.78;
            
            ptr.Get().N.Should().Be(123);
            ptr.Get().B.Should().Be(true);
            ptr.Get().X.Should().Be(456.78);
        }

        [Test]
        public void AllocateMultipleRecords_StateUpdated()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            var ptr1 = context.Buffer.Allocate();
            var ptr2 = context.Buffer.Allocate();
            var ptr3 = context.Buffer.Allocate();

            ptr2.ByteIndex.Should().Be(ptr1.ByteIndex + sizeof(ASimpleRecord));
            ptr3.ByteIndex.Should().Be(ptr2.ByteIndex + sizeof(ASimpleRecord));

            context.Buffer.InitialCapacity.Should().Be(10);
            context.Buffer.RecordCount.Should().Be(3);
            context.Buffer.TotalBytes.Should().Be(10 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(3 * sizeof(ASimpleRecord));
        }

        [Test]
        public void AllocateMultipleRecords_ValuesZeroed()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            var ptr1 = context.Buffer.Allocate();
            var ptr2 = context.Buffer.Allocate();
            var ptr3 = context.Buffer.Allocate();

            ptr1.Get().N.Should().Be(0);
            ptr1.Get().B.Should().Be(false);
            ptr1.Get().X.Should().Be(0);

            ptr2.Get().N.Should().Be(0);
            ptr2.Get().B.Should().Be(false);
            ptr2.Get().X.Should().Be(0);

            ptr3.Get().N.Should().Be(0);
            ptr3.Get().B.Should().Be(false);
            ptr3.Get().X.Should().Be(0);
        }

        [Test]
        public void StreamRoundtrip_BufferStateSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<ASimpleRecord> ptr1;
            void* rawPtr1Before;
            
            using (var context = new SingleBufferContext<ASimpleRecord>(capacity: 10))
            {
                ptr1 = context.Buffer.Allocate();
                context.Buffer.Allocate();
                context.Buffer.Allocate();

                rawPtr1Before = Unsafe.AsPointer(ref ptr1.Get());

                context.Buffer.WriteTo(stream);
            }

            stream.Length.Should().BeGreaterThan(3 * sizeof(ASimpleRecord));
            stream.Position = 0;

            using (var context = new SingleBufferContext<ASimpleRecord>(stream))
            {
                context.Buffer.ReadOnly.Should().BeTrue();
                context.Buffer.InitialCapacity.Should().Be(3);
                context.Buffer.RecordCount.Should().Be(3);
                context.Buffer.TotalBytes.Should().Be(3 * sizeof(ASimpleRecord));
                context.Buffer.AllocatedBytes.Should().Be(3 * sizeof(ASimpleRecord));

                void* rawPtr1After = Unsafe.AsPointer(ref ptr1.Get());
                (rawPtr1After != rawPtr1Before).Should().BeTrue();
            }
        }

        [Test]
        public void StreamRoundtrip_ScalarValuesSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<ASimpleRecord> ptr1, ptr2, ptr3;
            
            using (var context = new SingleBufferContext<ASimpleRecord>(capacity: 10))
            {
                ptr1 = context.Buffer.Allocate();
                ptr2 = context.Buffer.Allocate();
                ptr3 = context.Buffer.Allocate();

                ptr1.Get().N = 123;
                ptr1.Get().B = true;
                ptr1.Get().X = 111.22;

                ptr2.Get().N = 456;
                ptr2.Get().B = false;
                ptr2.Get().X = 333.44;

                ptr3.Get().N = 789;
                ptr3.Get().B = true;
                ptr3.Get().X = 555.66;
                
                context.Buffer.WriteTo(stream);
            }

            stream.Position = 0;

            using (var context = new SingleBufferContext<ASimpleRecord>(stream))
            {
                ptr1.Get().N.Should().Be(123);
                ptr1.Get().B.Should().Be(true);
                ptr1.Get().X.Should().Be(111.22);

                ptr2.Get().N.Should().Be(456);
                ptr2.Get().B.Should().Be(false);
                ptr2.Get().X.Should().Be(333.44);

                ptr3.Get().N.Should().Be(789);
                ptr3.Get().B.Should().Be(true);
                ptr3.Get().X.Should().Be(555.66);
            }

        }

        [Test]
        public void MultipleRecordTypesStreamRoundtrip_BufferStateSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<ARecordWithPointers> ptr1, ptr2;
            void* rawPtr1Before;

            var contextBefore = new BufferContext(typeof(ASimpleRecord), typeof(ARecordWithPointers));
            using (var scope = new BufferContextScope(contextBefore))
            {
                ptr1 = scope.GetBuffer<ARecordWithPointers>().Allocate(new ARecordWithPointers {
                    Simple1 = scope.GetBuffer<ASimpleRecord>().Allocate(),
                    Simple2 = scope.GetBuffer<ASimpleRecord>().Allocate(),
                });
                ptr2 = scope.GetBuffer<ARecordWithPointers>().Allocate(new ARecordWithPointers {
                    Simple1 = scope.GetBuffer<ASimpleRecord>().Allocate(),
                    Simple2 = scope.GetBuffer<ASimpleRecord>().Allocate(),
                });
                rawPtr1Before = Unsafe.AsPointer(ref ptr1.Get());
                contextBefore.WriteTo(stream);
            }
            
            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (var scope = new BufferContextScope(contextAfter))
            {
                scope.GetBuffer<ARecordWithPointers>().Should().BeSameAs(
                    contextAfter.GetBuffer<ARecordWithPointers>()
                );
                scope.GetBuffer<ARecordWithPointers>().Should().NotBeSameAs(
                    contextBefore.GetBuffer<ARecordWithPointers>()
                );

                void* rawPtr1After = Unsafe.AsPointer(ref ptr1.Get());
                (rawPtr1After != rawPtr1Before).Should().BeTrue();

                contextAfter.RecordTypeCount.Should().Be(2);
                contextAfter.RecordTypes.Should().BeEquivalentTo(new[] {
                    typeof(ASimpleRecord), 
                    typeof(ARecordWithPointers)
                });

                var buffer1 = contextAfter.GetBuffer<ASimpleRecord>();
                buffer1.ReadOnly.Should().BeTrue();
                buffer1.InitialCapacity.Should().Be(4);
                buffer1.RecordCount.Should().Be(4);
                buffer1.TotalBytes.Should().Be(4 * sizeof(ASimpleRecord));
                buffer1.AllocatedBytes.Should().Be(4 * sizeof(ASimpleRecord));

                var buffer2 = contextAfter.GetBuffer<ARecordWithPointers>();
                buffer2.ReadOnly.Should().BeTrue();
                buffer2.InitialCapacity.Should().Be(2);
                buffer2.RecordCount.Should().Be(2);
                buffer2.TotalBytes.Should().Be(2 * sizeof(ASimpleRecord));
                buffer2.AllocatedBytes.Should().Be(2 * sizeof(ASimpleRecord));
            }
        }

        [Test]
        public void MultipleRecordTypesStreamRoundtrip_PointersAcrossRecordTypesSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<ARecordWithPointers> ptr1, ptr2;

            var contextBefore = new BufferContext(typeof(ASimpleRecord), typeof(ARecordWithPointers));
            using (var scope = new BufferContextScope(contextBefore))
            {
                ptr1 = scope.GetBuffer<ARecordWithPointers>().Allocate(new ARecordWithPointers {
                    Num1 = 123,
                    Simple1 = scope.GetBuffer<ASimpleRecord>().Allocate(new ASimpleRecord() {
                        N = 11, B = true, X = 22.2 
                    }),
                    Num2 = 456,
                    Simple2 = scope.GetBuffer<ASimpleRecord>().Allocate(new ASimpleRecord() {
                        N = 33, B = false, X = 44.4 
                    }),
                });

                ptr2 = scope.GetBuffer<ARecordWithPointers>().Allocate(new ARecordWithPointers {
                    Num1 = 789,
                    Simple1 = scope.GetBuffer<ASimpleRecord>().Allocate(new ASimpleRecord() {
                        N = 55, B = false, X = 66.6 
                    }),
                    Num2 = 101112,
                    Simple2 = scope.GetBuffer<ASimpleRecord>().Allocate(new ASimpleRecord() {
                        N = 77, B = true, X = 88.8 
                    }),
                });
                
                contextBefore.WriteTo(stream);
            }
            
            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (var scope = new BufferContextScope(contextAfter))
            {
                ptr1.Get().Num1.Should().Be(123);
                ptr1.Get().Simple1.Get().N.Should().Be(11);
                ptr1.Get().Simple1.Get().B.Should().Be(true);
                ptr1.Get().Simple1.Get().X.Should().Be(22.2);
                ptr1.Get().Num2.Should().Be(456);
                ptr1.Get().Simple2.Get().N.Should().Be(33);
                ptr1.Get().Simple2.Get().B.Should().Be(false);
                ptr1.Get().Simple2.Get().X.Should().Be(44.4);
                
                ptr2.Get().Num1.Should().Be(789);
                ptr2.Get().Simple1.Get().N.Should().Be(55);
                ptr2.Get().Simple1.Get().B.Should().Be(false);
                ptr2.Get().Simple1.Get().X.Should().Be(66.6);
                ptr2.Get().Num2.Should().Be(101112);
                ptr2.Get().Simple2.Get().N.Should().Be(77);
                ptr2.Get().Simple2.Get().B.Should().Be(true);
                ptr2.Get().Simple2.Get().X.Should().Be(88.8);
            }
        }
        
        [Test]
        public void VariableSizeRecord_StreamRoundtrip_BufferStateSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<AVariableSizeRecord> ptr1;
            void* rawPtr1Before;

            var contextBefore = new BufferContext(typeof(AVariableSizeRecord));
            using (var scope = new BufferContextScope(contextBefore))
            {
                ptr1 = scope.GetBuffer<AVariableSizeRecord>().Allocate(new AVariableSizeRecord(5));
                scope.GetBuffer<AVariableSizeRecord>().Allocate(new AVariableSizeRecord(7));
                scope.GetBuffer<AVariableSizeRecord>().Allocate(new AVariableSizeRecord(3));
                
                var buffer = contextBefore.GetBuffer<AVariableSizeRecord>();
                buffer.InitialCapacity.Should().Be(BufferContext.DefaultBufferCapacity);
                buffer.RecordCount.Should().Be(3);
                buffer.TotalBytes.Should().Be(BufferContext.DefaultBufferCapacity * sizeof(AVariableSizeRecord));
                buffer.AllocatedBytes.Should().Be(3 * sizeof(AVariableSizeRecord) + (4 + 6 + 2) * sizeof(int));
                
                rawPtr1Before = Unsafe.AsPointer(ref ptr1.Get());
                contextBefore.WriteTo(stream);
            }
            
            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (var scope = new BufferContextScope(contextAfter))
            {
                void* rawPtr1After = Unsafe.AsPointer(ref ptr1.Get());
                (rawPtr1After != rawPtr1Before).Should().BeTrue();

                var buffer = contextAfter.GetBuffer<AVariableSizeRecord>();
                buffer.ReadOnly.Should().BeTrue();
                buffer.InitialCapacity.Should().Be(3);
                buffer.RecordCount.Should().Be(3);
                buffer.TotalBytes.Should().Be(buffer.TotalBytes);
                buffer.AllocatedBytes.Should().Be(3 * sizeof(AVariableSizeRecord) + (4 + 6 + 2) * sizeof(int));
            }
        }
        
        [Test]
        public void VariableSizeRecord_StreamRoundtrip_DataSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<AVariableSizeRecord> ptr1, ptr2, ptr3;

            var contextBefore = new BufferContext(typeof(AVariableSizeRecord));
            using (var scope = new BufferContextScope(contextBefore))
            {
                ptr1 = scope.GetBuffer<AVariableSizeRecord>().Allocate(new AVariableSizeRecord(5));
                ptr2 = scope.GetBuffer<AVariableSizeRecord>().Allocate(new AVariableSizeRecord(7));
                ptr3 = scope.GetBuffer<AVariableSizeRecord>().Allocate(new AVariableSizeRecord(3));

                ptr1.Get().SetNumbers(new[] { 11, 22, 33, 44, 55 });
                ptr2.Get().SetNumbers(new[] { 7, 6, 5, 4, 3, 2, 1 });
                ptr3.Get().SetNumbers(new[] { 0xFFFFF, 0xEEEEE, 0xDDDDD });
                
                contextBefore.WriteTo(stream);
            }
            
            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (var scope = new BufferContextScope(contextAfter))
            {
                ptr1.Get().GetNumbers().Should().Equal(new[] { 11, 22, 33, 44, 55 });
                ptr2.Get().GetNumbers().Should().Equal(new[] { 7, 6, 5, 4, 3, 2, 1 });
                ptr3.Get().GetNumbers().Should().Equal(new[] { 0xFFFFF, 0xEEEEE, 0xDDDDD });
            }
        }

        public struct ASimpleRecord
        {
            public int N { get; set; }
            public bool B { get; set; }
            public double X { get; set; }
        }

        public readonly struct ARecordWithPointers
        {
            public int Num1 { get; init; }
            public BufferPtr<ASimpleRecord> Simple1 { get; init; }
            public int Num2 { get; init; }
            public BufferPtr<ASimpleRecord> Simple2 { get; init; }
        }

        public struct AVariableSizeRecord : IVariableSizeRecord
        {
            public readonly int Count;
            private fixed int _numbers[1];

            public AVariableSizeRecord(int count)
            {
                Count = count;
                _numbers[0] = 0;
            }

            public int SizeOf()
            {
                return SizeOf(Count);
            }

            public int[] GetNumbers()
            {
                var result = new int[Count];
                
                fixed (int* pDest = &_numbers[0])
                {
                    for (int i = 0; i < Count; i++)
                    {
                        result[i] = pDest[i];
                    }
                }

                return result;
            }
            
            public void SetNumbers(int[] numbers)
            {
                if (numbers.Length != Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(numbers), "Array length must match Count");
                }
                
                fixed (int* pSrc = &numbers[0])
                {
                    fixed (int* pDest = &_numbers[0])
                    {
                        Buffer.MemoryCopy(
                            pSrc, 
                            pDest, 
                            Count * sizeof(int),
                            Count * sizeof(int));
                    }
                }
            }

            public static int SizeOf(int numberCount)
            {
                return sizeof(AVariableSizeRecord) + (numberCount - 1) * sizeof(int);
            }
        }
    }
}
