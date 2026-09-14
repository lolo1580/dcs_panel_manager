namespace DCSPanel.DCSBIOS.Protocol;

public sealed record DcsBiosWrite(ushort Address, ReadOnlyMemory<byte> Data);

/// <summary>
/// Incremental parser for the official DCS-BIOS binary export protocol.
/// It accepts arbitrarily split buffers and ignores data until the first frame marker.
/// </summary>
public sealed class DcsBiosProtocolParser
{
    private const byte SyncByte = 0x55;
    private const int SyncLength = 4;
    private readonly List<byte> _data = [];
    private ParseState _state = ParseState.AddressLow;
    private int _pendingSyncBytes;
    private ushort _address;
    private ushort _count;

    public event EventHandler? FrameSynchronized;
    public event EventHandler<DcsBiosWrite>? WriteReceived;

    public bool IsSynchronized { get; private set; }
    public long TotalWrites { get; private set; }

    public void Process(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (value == SyncByte)
            {
                _pendingSyncBytes++;
                if (_pendingSyncBytes == SyncLength)
                {
                    SynchronizeFrame();
                    _pendingSyncBytes = 0;
                }

                continue;
            }

            while (_pendingSyncBytes > 0)
            {
                ProcessDataByte(SyncByte);
                _pendingSyncBytes--;
            }

            ProcessDataByte(value);
        }
    }

    public void Reset()
    {
        IsSynchronized = false;
        _pendingSyncBytes = 0;
        _state = ParseState.AddressLow;
        _address = 0;
        _count = 0;
        _data.Clear();
    }

    private void SynchronizeFrame()
    {
        IsSynchronized = true;
        _state = ParseState.AddressLow;
        _address = 0;
        _count = 0;
        _data.Clear();
        FrameSynchronized?.Invoke(this, EventArgs.Empty);
    }

    private void ProcessDataByte(byte value)
    {
        if (!IsSynchronized)
        {
            return;
        }

        switch (_state)
        {
            case ParseState.AddressLow:
                _address = value;
                _state = ParseState.AddressHigh;
                break;
            case ParseState.AddressHigh:
                _address |= (ushort)(value << 8);
                _state = ParseState.CountLow;
                break;
            case ParseState.CountLow:
                _count = value;
                _state = ParseState.CountHigh;
                break;
            case ParseState.CountHigh:
                _count |= (ushort)(value << 8);
                if (_count == 0 || _address + _count > ushort.MaxValue + 1)
                {
                    Reset();
                    break;
                }

                _data.Clear();
                _state = ParseState.Data;
                break;
            case ParseState.Data:
                _data.Add(value);
                if (_data.Count == _count)
                {
                    TotalWrites++;
                    WriteReceived?.Invoke(this, new DcsBiosWrite(_address, _data.ToArray()));
                    _state = ParseState.AddressLow;
                    _address = 0;
                    _count = 0;
                    _data.Clear();
                }

                break;
        }
    }

    private enum ParseState
    {
        AddressLow,
        AddressHigh,
        CountLow,
        CountHigh,
        Data
    }
}
