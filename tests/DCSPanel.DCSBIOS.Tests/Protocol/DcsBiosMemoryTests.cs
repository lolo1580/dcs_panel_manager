using DCSPanel.DCSBIOS.Protocol;

namespace DCSPanel.DCSBIOS.Tests.Protocol;

public sealed class DcsBiosMemoryTests
{
    [Fact]
    public void ReadNullTerminatedAsciiReflectsPartialWrites()
    {
        var memory = new DcsBiosMemory();
        memory.Apply(new DcsBiosWrite(0x0000, "F-16"u8.ToArray()));
        memory.Apply(new DcsBiosWrite(0x0004, new byte[] { 0x43, 0x5F, 0x35, 0x30, 0x00 }));

        Assert.Equal("F-16C_50", memory.ReadNullTerminatedAscii(0x0000, 24));
    }
}
