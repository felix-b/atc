using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Atc.Data.Tests
{
    [TestFixture]
    public unsafe class StringDataTests
    {
        [Test]
        public void PointerRoundtripTest()
        {
            var structSize = Marshal.SizeOf(typeof(StringData));
            var original = new StringData("abcd");
            //var byteArray = new byte[structSize];
            //Buffer.MemoryCopy(original, pDest);

            Console.WriteLine("---sizeof---");
            Console.WriteLine(Marshal.SizeOf(original));
            Console.WriteLine(Marshal.SizeOf(typeof(StringData)));

            Console.WriteLine("---original dump---");

            fixed (byte* pOriginal = original)
            {
                Console.WriteLine($"Address: 0x{new IntPtr(pOriginal).ToInt64():X}");
                
                for (int i = 0; i < 24; i++)
                {
                    byte b = pOriginal[i];
                    Console.WriteLine($"[{i}] 0x{b:X}");
                }
            }

            Console.WriteLine("---roundtrip dump---");

            fixed (byte* pSrc = original)
            {
                byte* pDest = pSrc;
                ref StringData casted = ref Unsafe.AsRef<StringData>(pDest);

                fixed (byte* pRound = casted)
                {
                    Console.WriteLine($"Address: 0x{new IntPtr(pRound).ToInt64():X}");
                    
                    for (int i = 0; i < 24; i++)
                    {
                        byte b = pRound[i];
                        Console.WriteLine($"[{i}] 0x{b:X}");
                    }
                }

                Assert.That(casted._length, Is.EqualTo(4));
                Assert.That(casted._inflated, Is.Null);
                Assert.That(casted._chars[0], Is.EqualTo('a'));
                Assert.That(casted._chars[1], Is.EqualTo('b'));
                Assert.That(casted._chars[2], Is.EqualTo('c'));
                Assert.That(casted._chars[3], Is.EqualTo('d'));
                Assert.That(casted._chars[4], Is.EqualTo('\0'));
            }
            
            

        }
        
        [Test]
        public void CopyRoundtripTest()
        {
            var structSize = Marshal.SizeOf(typeof(StringData));
            var original = new StringData("abcd");
            var byteArray = new byte[structSize];

            fixed (byte* pOriginal = original)
            {
                fixed (byte* pByteArray = &byteArray[0])
                {
                    Buffer.MemoryCopy(pOriginal, pByteArray, structSize, structSize);

                    Console.WriteLine("---dump comparison---");
                    Console.WriteLine($"Address: 0x{new IntPtr(pOriginal).ToInt64():X} 0x{new IntPtr(pByteArray).ToInt64():X}");
                
                    for (int i = 0; i < 24; i++)
                    {
                        byte b1 = pOriginal[i];
                        byte b2 = pByteArray[i];
                        Console.WriteLine($"[{i}] 0x{b1:X} 0x{b2:X}");
                    }
                    
                    ref StringData casted = ref Unsafe.AsRef<StringData>(pByteArray);
                }
            }
        }
    }
}