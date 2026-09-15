using System.Text;

namespace DCSPanel.DCSBIOS.Protocol;

public sealed class DcsBiosMemory
{
    private readonly byte[] _memory = new byte[ushort.MaxValue + 1];
    private readonly object _gate = new();

    public void Apply(DcsBiosWrite write)
    {
        if (write.Address + write.Data.Length > _memory.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(write), "The write exceeds the DCS-BIOS address space.");
        }

        lock (_gate)
        {
            write.Data.Span.CopyTo(_memory.AsSpan(write.Address));
        }
    }

    public string ReadNullTerminatedAscii(ushort address, int maximumLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        if (address + maximumLength > _memory.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }

        lock (_gate)
        {
            var source = _memory.AsSpan(address, maximumLength);
            var terminator = source.IndexOf((byte)0);
            return Encoding.ASCII.GetString(terminator >= 0 ? source[..terminator] : source).Trim();
        }
    }

    public ushort ReadUnsignedWord(ushort address)
    {
        if (address > ushort.MaxValue - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(address));
        }

        lock (_gate)
        {
            return (ushort)(_memory[address] | (_memory[address + 1] << 8));
        }
    }
}
