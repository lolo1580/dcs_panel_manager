using DCSPanel.DCSBIOS.Protocol;

namespace DCSPanel.DCSBIOS.Tests.Protocol;

public sealed class DcsBiosProtocolParserTests
{
    [Fact]
    public void ProcessIgnoresBytesUntilSynchronizationSequence()
    {
        var parser = new DcsBiosProtocolParser();
        var writes = new List<DcsBiosWrite>();
        parser.WriteReceived += (_, write) => writes.Add(write);

        parser.Process([0x00, 0x10, 0x02, 0x00, 0x01, 0x02]);

        Assert.Empty(writes);
        Assert.False(parser.IsSynchronized);
    }

    [Fact]
    public void ProcessParsesWritesAcrossArbitraryBufferBoundaries()
    {
        var parser = new DcsBiosProtocolParser();
        var writes = new List<DcsBiosWrite>();
        parser.WriteReceived += (_, write) => writes.Add(write);

        parser.Process([0x55, 0x55]);
        parser.Process([0x55, 0x55, 0x00, 0x10, 0x04]);
        parser.Process([0x00, 0x41, 0x2D, 0x31, 0x30]);

        var write = Assert.Single(writes);
        Assert.Equal(0x1000, write.Address);
        Assert.Equal([0x41, 0x2D, 0x31, 0x30], write.Data.ToArray());
    }

    [Fact]
    public void ProcessPreservesUpToThreeConsecutiveDataBytesWithSyncValue()
    {
        var parser = new DcsBiosProtocolParser();
        DcsBiosWrite? received = null;
        parser.WriteReceived += (_, write) => received = write;

        parser.Process([0x55, 0x55, 0x55, 0x55, 0x00, 0x20, 0x04, 0x00, 0x55, 0x55, 0x55, 0x01]);

        Assert.NotNull(received);
        Assert.Equal([0x55, 0x55, 0x55, 0x01], received.Data.ToArray());
    }
}
