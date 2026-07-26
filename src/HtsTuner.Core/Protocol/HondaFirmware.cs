using System;

namespace HtsTuner.Core.Protocol;

// Clean-room reimplementation of the Honda real-time datalogging firmware
// protocol used by the original HTS "Snake" datalogger board / HULOG cable.
//
// These values were recovered by studying the byte-level serial exchange, not
// copied source: the board is driven with short ASCII commands and replies with
// a bitmask status word plus tagged data bytes.

/// <summary>Commands the host sends to the datalogger firmware.</summary>
public enum UserRequest : byte
{
    Nop = 0,
    Online = 1,
    Offline = 2,
    GetConfig = 3,
    Erase = 4,
    Program = 5,
    Feature = 6,
}

/// <summary>Status bitmask the firmware replies with.</summary>
[Flags]
public enum FirmwareResponse : uint
{
    None = 0u,
    Ok = 0x1u,
    Ack = 0x2u,
    Ready = 0x4u,
    Fail = 0x8u,
    Status = 0x10u,
    FirmwareVersion = 0x20u,
    Data = 0x40u,
    Progress = 0x80u,
    CommandError = 0x100u,
    Busy = 0x200u,
    Abort = 0x400u,
    WriteCongested = 0x800u,
    Unknown = 0x1000u,
    Boot = 0x2000u,

    WriteMask = Ack | WriteCongested,               // 0x882
    WatchdogMask = Ack | Busy | WriteCongested,      // 0x8C2
    ErrorMask = Fail | CommandError | Abort,         // 0x708
    UnexpectedMask = Unknown | Boot,                 // 0x3000
    OkMask = 0xF7u,
}

/// <summary>
/// Tag bytes that prefix each value in the live datalog stream. Every tag is
/// immediately followed by a single data byte carrying that channel's value.
/// </summary>
public static class DatalogTags
{
    public const byte Ack = 0xAF;      // 175 - handshake ack + channel A marker
    public const byte ChannelB = 0xBF; // 191
    public const byte ChannelC = 0xCF; // 207
}
