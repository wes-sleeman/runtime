// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;
using DATAGRAM_RECEIVED = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._DATAGRAM_RECEIVED_e__Struct;

namespace System.Net.Quic;

public sealed partial class QuicConnection
{
    public event EventHandler<DatagramReceivedEventArgs>? DatagramRecevied;

    private unsafe int HandleDatagramReceived(ref DATAGRAM_RECEIVED data)
    {
        if (DatagramRecevied is not null)
        {
            DatagramReceivedEventArgs e = new() { Buffer = data.Buffer->Span.ToArray() };
            DatagramRecevied.Invoke(this, e);
        }

        return QUIC_STATUS_SUCCESS;
    }
    public void SendDatagram(byte[] buffer, int offset, int count) => SendDatagram(buffer, offset, count, SendDatagramOptions.None);
    public void SendDatagram(byte[] buffer, int offset, int count, SendDatagramOptions options) => SendDatagram(buffer.AsMemory(offset, count), options);
    public void SendDatagram(ReadOnlyMemory<byte> buffer) => SendDatagram(buffer, SendDatagramOptions.None);
    public void SendDatagram(ReadOnlyMemory<byte> buffer, SendDatagramOptions options)
    {
        MsQuicBuffers bufferBuilder = new();
        bufferBuilder.Initialize(buffer);

        int sendResult;
        unsafe
        {
            sendResult = MsQuicApi.Api.DatagramSend(_handle, bufferBuilder.Buffers, 1, (QUIC_SEND_FLAGS)options, context: (void*)null);
        }

        ThrowHelper.ThrowIfMsQuicError(sendResult, "SendDatagram failed");
    }
}

public sealed class DatagramReceivedEventArgs : System.EventArgs
{
    public required byte[] Buffer { get; init; }
}

/// <summary>
/// Specifies options for sending datagrams over a <see cref="QuicConnection"/>.
/// </summary>
/// <remarks>
/// Accepted by <see cref="QuicConnection.SendDatagram(ReadOnlyMemory{byte}, SendDatagramOptions)"/> and its various overloads.
/// See the <see href="https://microsoft.github.io/msquic/msquicdocs/docs/api/DatagramSend.html">MsQuic documentation</see> for a more in-depth explanation of flag behavior.
/// </remarks>
[Flags]
public enum SendDatagramOptions
{
    /// <summary>
    /// No special behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the data is allowed to be sent in 0-RTT, if available.
    /// </summary>
    Allow0Rtt = 1,

    /// <summary>
    /// Marks the datagram as a priority to ensure a it is sent before others.
    /// </summary>
    DatagramPriority = 8,

    /// <summary>
    /// Allows the datagram to be dropped if it is still remaining in the queue
    /// after all sendable data has been flushed to the underlying connection.
    /// </summary>
    CancelOnBlocked = 64
}
