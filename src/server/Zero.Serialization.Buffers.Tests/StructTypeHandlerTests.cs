using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public unsafe class StructTypeHandlerTests
    {
        [Test]
        public void LayoutOfTrivialStruct()
        {
            sizeof(ATrivialStruct).Should().Be(8);

            var handler = new StructTypeHandler(typeof(ATrivialStruct));

            handler.Size.Should().Be(8);
            handler.IsVariableSize.Should().Be(false);
            handler.Fields.Count.Should().Be(2);
            
            handler.Fields[0].Name.Should().Be("X");
            handler.Fields[0].Offset.Should().Be(0);
            handler.Fields[0].Size.Should().Be(4);
            handler.Fields[0].Type.Should().BeSameAs(typeof(int));
            handler.Fields[0].IsVariableBuffer.Should().Be(false);
            handler.Fields[0].ValueTypeHandler.Should().Be(null);

            handler.Fields[1].Name.Should().Be("Y");
            handler.Fields[1].Offset.Should().Be(4);
            handler.Fields[1].Size.Should().Be(4);
            handler.Fields[1].Type.Should().BeSameAs(typeof(int));
            handler.Fields[1].IsVariableBuffer.Should().Be(false);
            handler.Fields[1].ValueTypeHandler.Should().Be(null);
        }

        [Test]
        public void ValuesOfTrivialStruct()
        {
            var handler = new StructTypeHandler(typeof(ATrivialStruct));
            var instance = new ATrivialStruct {X = 123, Y = -1};
            
            var values = handler.GetFieldValues(&instance);

            values.Length.Should().Be(2);
            values[0].Field.Should().BeSameAs(handler.Fields[0]);
            values[0].Value.Should().BeOfType<int>();
            values[0].Value.Should().Be(123);

            values[1].Field.Should().BeSameAs(handler.Fields[1]);
            values[1].Value.Should().BeOfType<int>();
            values[1].Value.Should().Be(-1);
        }

        [Test]
        public void LayoutOfStructWithNesting()
        {
            var handler = new StructTypeHandler(typeof(AStructWithNesting));

            sizeof(AStructWithNesting).Should().Be(24);

            handler.Size.Should().Be(24);
            handler.IsVariableSize.Should().Be(false);
            handler.Fields.Count.Should().Be(3);
            
            handler.Fields[0].Name.Should().Be("Bool");
            handler.Fields[0].Offset.Should().Be(0);
            handler.Fields[0].Size.Should().Be(1);
            handler.Fields[0].Type.Should().BeSameAs(typeof(bool));
            handler.Fields[0].IsVariableBuffer.Should().Be(false);
            handler.Fields[0].ValueTypeHandler.Should().Be(null);

            handler.Fields[1].Name.Should().Be("Nested");
            handler.Fields[1].Offset.Should().Be(4);
            handler.Fields[1].Size.Should().Be(8);
            handler.Fields[1].Type.Should().BeSameAs(typeof(ATrivialStruct));
            handler.Fields[1].IsVariableBuffer.Should().Be(false);
            handler.Fields[1].ValueTypeHandler.Should().NotBeNull();
            handler.Fields[1].ValueTypeHandler!.Type.Should().BeSameAs(typeof(ATrivialStruct));

            handler.Fields[2].Name.Should().Be("X");
            handler.Fields[2].Offset.Should().Be(16);
            handler.Fields[2].Size.Should().Be(8);
            handler.Fields[2].Type.Should().BeSameAs(typeof(double));
            handler.Fields[2].IsVariableBuffer.Should().Be(false);
            handler.Fields[2].ValueTypeHandler.Should().BeNull();
        }

        [Test]
        public void ValuesOfStructWithNesting()
        {
            var handler = new StructTypeHandler(typeof(AStructWithNesting));
            var instance = new AStructWithNesting {
                Bool = true,
                Nested = new ATrivialStruct {
                    X = 123, 
                    Y = -1
                },
                X = 456.789
            };
            
            var values = handler.GetFieldValues(&instance);

            values.Length.Should().Be(3);
            values[0].Field.Should().BeSameAs(handler.Fields[0]);
            values[0].Value.Should().BeOfType<bool>();
            values[0].Value.Should().Be(true);

            values[1].Field.Should().BeSameAs(handler.Fields[1]);
            values[1].Value.Should().BeOfType<StructTypeHandler.FieldValuePair[]>();
            
            var nestedValues = (StructTypeHandler.FieldValuePair[])values[1].Value!;
            nestedValues!.Length.Should().Be(2);
            nestedValues[0].Field.Name.Should().Be("X");
            nestedValues[0].Value.Should().Be(123);
            nestedValues[1].Field.Name.Should().Be("Y");
            nestedValues[1].Value.Should().Be(-1);

            values[2].Field.Should().BeSameAs(handler.Fields[2]);
            values[2].Value.Should().BeOfType<double>();
            values[2].Value.Should().Be(456.789);
        }

        [Test]
        public void LayoutOfClosedGenericStructType()
        {
            var handler = new StructTypeHandler(typeof(AGenericStruct<DayOfWeek>));

            sizeof(AGenericStruct<DayOfWeek>).Should().Be(12);

            handler.Size.Should().Be(12);
            handler.IsVariableSize.Should().Be(false);
            handler.Fields.Count.Should().Be(3);
            
            handler.Fields[0].Name.Should().Be("Bool");
            handler.Fields[0].Offset.Should().Be(0);
            handler.Fields[0].Size.Should().Be(1);
            handler.Fields[0].Type.Should().BeSameAs(typeof(bool));
            handler.Fields[0].IsVariableBuffer.Should().Be(false);
            handler.Fields[0].ValueTypeHandler.Should().Be(null);

            handler.Fields[1].Name.Should().Be("Value");
            handler.Fields[1].Offset.Should().Be(4);
            handler.Fields[1].Size.Should().Be(4);
            handler.Fields[1].Type.Should().BeSameAs(typeof(DayOfWeek));
            handler.Fields[1].IsVariableBuffer.Should().Be(false);
            handler.Fields[1].ValueTypeHandler.Should().BeNull();

            handler.Fields[2].Name.Should().Be("N");
            handler.Fields[2].Offset.Should().Be(8);
            handler.Fields[2].Size.Should().Be(4);
            handler.Fields[2].Type.Should().BeSameAs(typeof(int));
            handler.Fields[2].IsVariableBuffer.Should().Be(false);
            handler.Fields[2].ValueTypeHandler.Should().BeNull();
        }

        [Test]
        public void ValuesOfClosedGenericStructType()
        {
            var handler = new StructTypeHandler(typeof(AGenericStruct<DayOfWeek>));
            var instance = new AGenericStruct<DayOfWeek> {
                Bool = true,
                Value = DayOfWeek.Tuesday,
                N = 123
            };
            
            var values = handler.GetFieldValues(&instance);

            values.Length.Should().Be(3);
            values[0].Field.Should().BeSameAs(handler.Fields[0]);
            values[0].Value.Should().BeOfType<bool>();
            values[0].Value.Should().Be(true);

            values[1].Field.Should().BeSameAs(handler.Fields[1]);
            values[1].Value.Should().BeOfType<DayOfWeek>();
            values[1].Value.Should().Be(DayOfWeek.Tuesday);

            values[2].Field.Should().BeSameAs(handler.Fields[2]);
            values[2].Value.Should().BeOfType<int>();
            values[2].Value.Should().Be(123);
        }
        
        [Test]
        public void LayoutOfStructWithNestedGeneric()
        {
            var handler = new StructTypeHandler(typeof(AStructWithNestedGeneric));

            sizeof(AStructWithNestedGeneric).Should().Be(24);

            handler.Size.Should().Be(24);
            handler.IsVariableSize.Should().Be(false);
            handler.Fields.Count.Should().Be(3);
            
            handler.Fields[0].Name.Should().Be("Bool");
            handler.Fields[0].Offset.Should().Be(0);
            handler.Fields[0].Size.Should().Be(1);
            handler.Fields[0].Type.Should().BeSameAs(typeof(bool));
            handler.Fields[0].IsVariableBuffer.Should().Be(false);
            handler.Fields[0].ValueTypeHandler.Should().Be(null);

            handler.Fields[1].Name.Should().Be("Nested");
            handler.Fields[1].Offset.Should().Be(4);
            handler.Fields[1].Size.Should().Be(16);
            handler.Fields[1].Type.Should().BeSameAs(typeof(AGenericStruct<ATrivialStruct>));
            handler.Fields[1].IsVariableBuffer.Should().Be(false);
            handler.Fields[1].ValueTypeHandler.Should().NotBeNull();
            handler.Fields[1].ValueTypeHandler!.Type.Should().BeSameAs(typeof(AGenericStruct<ATrivialStruct>));

            handler.Fields[2].Name.Should().Be("N");
            handler.Fields[2].Offset.Should().Be(20);
            handler.Fields[2].Size.Should().Be(4);
            handler.Fields[2].Type.Should().BeSameAs(typeof(int));
            handler.Fields[2].IsVariableBuffer.Should().Be(false);
            handler.Fields[2].ValueTypeHandler.Should().BeNull();
        }

        [Test]
        public void ValuesOfStructWithNestedGeneric()
        {
            var handler = new StructTypeHandler(typeof(AStructWithNestedGeneric));
            var instance = new AStructWithNestedGeneric {
                Bool = true,
                Nested = new AGenericStruct<ATrivialStruct> {
                    Bool = true,
                    Value = new ATrivialStruct {
                        X = 123, 
                        Y = 456
                    },
                    N = 789
                },
                N = 101112
            };
            
            var values = handler.GetFieldValues(&instance);

            values.Length.Should().Be(3);
            values[0].Field.Should().BeSameAs(handler.Fields[0]);
            values[0].Value.Should().BeOfType<bool>();
            values[0].Value.Should().Be(true);

            values[1].Field.Should().BeSameAs(handler.Fields[1]);
            values[1].Value.Should().BeOfType<StructTypeHandler.FieldValuePair[]>();
            
            var subValues = (StructTypeHandler.FieldValuePair[])values[1].Value!;
            subValues!.Length.Should().Be(3);
            subValues[0].Field.Name.Should().Be("Bool");
            subValues[0].Value.Should().Be(true);
            subValues[1].Field.Name.Should().Be("Value");
            subValues[1].Value.Should().BeOfType<StructTypeHandler.FieldValuePair[]>();
            
            var subSubValues = (StructTypeHandler.FieldValuePair[])subValues[1].Value!;
            subSubValues!.Length.Should().Be(2);
            subSubValues[0].Field.Name.Should().Be("X");
            subSubValues[0].Value.Should().Be(123);
            subSubValues[1].Field.Name.Should().Be("Y");
            subSubValues[1].Value.Should().Be(456);
            
            subValues[2].Field.Name.Should().Be("N");
            subValues[2].Value.Should().Be(789);
            
            values[2].Field.Should().BeSameAs(handler.Fields[2]);
            values[2].Value.Should().BeOfType<int>();
            values[2].Value.Should().Be(101112);
        }
 
        [Test]
        public void LayoutOfStructWithVariableBuffer()
        {
            sizeof(AStructWithVariableBuffer).Should().Be(12);

            var handler = new StructTypeHandler(typeof(AStructWithVariableBuffer));

            handler.Size.Should().Be(12);
            handler.IsVariableSize.Should().Be(true);
            handler.Fields.Count.Should().Be(3);
            
            handler.Fields[0].Name.Should().Be("Len");
            handler.Fields[0].Offset.Should().Be(0);
            handler.Fields[0].Size.Should().Be(4);
            handler.Fields[0].Type.Should().BeSameAs(typeof(int));
            handler.Fields[0].IsVariableBuffer.Should().Be(false);
            handler.Fields[0].ValueTypeHandler.Should().Be(null);

            handler.Fields[1].Name.Should().Be("B");
            handler.Fields[1].Offset.Should().Be(4);
            handler.Fields[1].Size.Should().Be(1);
            handler.Fields[1].Type.Should().BeSameAs(typeof(bool));
            handler.Fields[1].IsVariableBuffer.Should().Be(false);
            handler.Fields[1].ValueTypeHandler.Should().Be(null);

            handler.Fields[2].Name.Should().Be("Buf");
            handler.Fields[2].Offset.Should().Be(8);
            handler.Fields[2].Size.Should().Be(4);
            handler.Fields[2].Type.Should().BeSameAs(typeof(int[]));
            handler.Fields[2].IsVariableBuffer.Should().Be(true);
            handler.Fields[2].ValueTypeHandler.Should().Be(null);
        }

        [Test]
        public void ValuesOfStructWithVariableBuffer()
        {
            var handler = new StructTypeHandler(typeof(AStructWithVariableBuffer));
            byte* pBytes = stackalloc byte[128];
            
            ref AStructWithVariableBuffer instance = ref Unsafe.AsRef<AStructWithVariableBuffer>(pBytes); 
            instance.Initialize(new[] { 123, 456, 789, -1 });

            var values = handler.GetFieldValues(pBytes);

            values.Length.Should().Be(3);
            values[0].Field.Should().BeSameAs(handler.Fields[0]);
            values[0].Value.Should().BeOfType<int>();
            values[0].Value.Should().Be(4);

            values[1].Field.Should().BeSameAs(handler.Fields[1]);
            values[1].Value.Should().BeOfType<bool>();
            values[1].Value.Should().Be(true);

            values[2].Field.Should().BeSameAs(handler.Fields[2]);
            values[2].Value.Should().BeOfType<int[]>();
            values[2].Value.Should().BeEquivalentTo(new int[] { 123, 456, 789, -1 });
        }

        [Test]
        public void LayoutOfStringRecord()
        {
            Unsafe.SizeOf<StringRecord>().Should().Be(12);

            var handler = new StructTypeHandler(typeof(StringRecord));

            handler.Size.Should().Be(12);
            handler.IsVariableSize.Should().Be(true);
            handler.Fields.Count.Should().Be(3);
            
            handler.Fields[0].Name.Should().Be("_length");
            handler.Fields[0].Offset.Should().Be(0);
            handler.Fields[0].Size.Should().Be(4);
            handler.Fields[0].Type.Should().BeSameAs(typeof(int));
            handler.Fields[0].IsVariableBuffer.Should().Be(false);
            handler.Fields[0].ValueTypeHandler.Should().Be(null);

            handler.Fields[1].Name.Should().Be("_inflatedHandle");
            handler.Fields[1].Offset.Should().Be(4);
            handler.Fields[1].Size.Should().Be(4);
            handler.Fields[1].Type.Should().BeSameAs(typeof(int));
            handler.Fields[1].IsVariableBuffer.Should().Be(false);
            handler.Fields[1].ValueTypeHandler.Should().Be(null);

            handler.Fields[2].Name.Should().Be("_chars");
            handler.Fields[2].Offset.Should().Be(8);
            handler.Fields[2].Size.Should().Be(2);
            handler.Fields[2].Type.Should().BeSameAs(typeof(char[]));
            handler.Fields[2].IsVariableBuffer.Should().Be(true);
            handler.Fields[2].ValueTypeHandler.Should().Be(null);
        }

        [Test]
        public void ValuesOfStringRecord()
        {
            var handler = new StructTypeHandler(typeof(StringRecord));
            byte* pBytes = stackalloc byte[128];
            for (int i = 0; i < 128; i++)
            {
                pBytes[i] = 0xDB;
            }
            
            ref StringRecord instance = ref Unsafe.AsRef<StringRecord>(pBytes); 
            instance.SetValue("ABCD");

            var values = handler.GetFieldValues(pBytes);
            handler.GetInstanceSize(pBytes).Should().Be(16); 

            values.Length.Should().Be(3);
            values[0].Field.Should().BeSameAs(handler.Fields[0]);
            values[0].Value.Should().BeOfType<int>();
            values[0].Value.Should().Be(4);

            values[1].Field.Should().BeSameAs(handler.Fields[1]);
            values[1].Value.Should().Be(-1);

            values[2].Field.Should().BeSameAs(handler.Fields[2]);
            values[2].Value.Should().BeOfType<char[]>();
            values[2].Value.Should().BeEquivalentTo(new char[] { 'A', 'B', 'C', 'D' });
        }

        [Test, Ignore("Changes in progress in VectorRecord implementation")]
        public void LayoutOfVectorRecord()
        {
            //Unsafe.SizeOf<VectorRecord<MapRecordEntry>>().Should().Be(32); //now 40

            var handler = new StructTypeHandler(typeof(VectorRecord<MapRecordEntry<bool>>));

            handler.Size.Should().Be(32);
            handler.IsVariableSize.Should().Be(true);
            handler.Fields.Count.Should().Be(6);
        }

        [Test]
        public void CanInstantiateInfoProvider()
        {
            var handler = new StructTypeHandler(typeof(AStructWithInfoProvider));
            handler.BufferInfoProvider.Should().BeOfType<AStructWithInfoProvider.InfoProvider>();
        }

        public struct ATrivialStruct
        {
            public int X;
            public int Y;
        }

        public struct AStructWithNesting
        {
            public bool Bool;
            public ATrivialStruct Nested;
            public double X;
        }

        public struct AGenericStruct<T>
        {
            public bool Bool;
            public T Value;
            public int N;
        }
        
        public struct AStructWithNestedGeneric
        {
            public bool Bool;
            public AGenericStruct<ATrivialStruct> Nested;
            public int N;
        }

        public unsafe struct AStructWithVariableBuffer : IVariableSizeRecord
        {
            public int Len;
            public bool B;
            public fixed int Buf[1];

            public void Initialize(int[] initBuf)
            {
                Len = initBuf.Length;
                B = true;

                fixed (int* pIntBuf = &Buf[0])
                {
                    for (int i = 0; i < initBuf.Length; i++)
                    {
                        pIntBuf[i] = initBuf[i];
                    }
                }
            }
            
            public int SizeOf()
            {
                return sizeof(AStructWithVariableBuffer) + (Len - 1) * sizeof(int);
            }
        }

        [BufferInfoProvider(typeof(AStructWithInfoProvider.InfoProvider))]
        public struct AStructWithInfoProvider
        {
            public int Num;
            public bool Bool;

            public class InfoProvider : IBufferInfoProvider
            {
                public IEnumerable<(string Label, string Value)> GetInfo(ITypedBuffer buffer)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}