using System;
using System.IO;
using System.Linq;
using Atc.Telemetry.CodePath;
using FluentAssertions;

namespace Atc.Telemetry.Tests.CodePath;

public static class CodePathTestUtility
{
    public static void AssertNode(CodePathStreamReader.Node node, bool isSpan, string messageId, int childNodeCount)
    {
        node.IsSpan.Should().Be(isSpan);
        node.MessageId.Should().Be(messageId);
        node.Nodes.Count.Should().Be(childNodeCount);
    }

    public static CodePathStreamReader.Node AssertChildNode(
        CodePathStreamReader.Node parentNode, 
        string messageId, 
        bool isSpan, 
        int childNodeCount = 0)
    {
        var childNode = parentNode.Nodes.FirstOrDefault(n => n.MessageId == messageId);
        childNode.Should().NotBeNull(messageId);
        childNode!.IsSpan.Should().Be(isSpan);
        childNode.Nodes.Count.Should().Be(childNodeCount);
        return childNode;
    }

    public static CodePathStreamReader.Node ReadAll(CodePathTestDoubles.TestEnvironment environment)
    {
        var buffers = environment.Exporter.Buffers;
        var reader = new CodePathStreamReader();
        long totalSize = 0;

        Console.WriteLine("--------------stream contents------------");

        while (buffers.Reader.TryRead(out var buffer))
        {
            totalSize += buffer.Length;
            buffer.Position = 0;
            reader.ReadBuffer(buffer);
            
            PrintBuffer(buffer);
        }

        Console.WriteLine($"--------------end of contents, {totalSize} bytes------------");

        return reader.RootNode;
    }

    public static void PrintBuffer(MemoryStream buffer)
    {
        Console.WriteLine();

        buffer.Position = 0;
            
        for (int i = 0; i < buffer.Length; i++)
        {
            var byteValue = buffer.GetBuffer()[i];
            Console.Write($"{byteValue:X2} ");

            if ((i % 8) == 0)
            {
                Console.Write("   ");
            }

            if ((i % 16) == 0)
            {
                Console.WriteLine();
            }
        }

        Console.WriteLine();
        Console.WriteLine();
    }

    public static void PrintNodeTree(CodePathStreamReader.Node root)
    {
        Console.WriteLine("--------------node tree------------");

        PrintChildren(root);

        void PrintChildren(CodePathStreamReader.Node parent)
        {
            foreach (var child in parent.Nodes)
            {
                Console.WriteLine($"{child.Time:HH:mm:ss.fff} {new string('.', child.Depth * 2)}{child.MessageId}@{child.ThreadId} {child.Duration}");
                PrintChildren(child);
            }
        }

        Console.WriteLine("--------------end of node tree------------");
    }
}