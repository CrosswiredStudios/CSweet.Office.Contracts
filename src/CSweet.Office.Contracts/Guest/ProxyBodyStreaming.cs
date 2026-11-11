namespace CSweet.Office.Contracts.Guest;

/// <summary>
/// Bounds for guest protocol 1.2 streamed proxy bodies. A <see cref="ProxyRequest"/> or
/// <see cref="ProxyResponse"/> that sets <c>body_streamed</c> carries an empty inline body; its content
/// follows as <see cref="ProxyBodyFrame"/> messages for the same <c>request_id</c>, ending with END.
/// </summary>
public static class ProxyBodyStreaming
{
    /// <summary>Largest payload of one DATA frame. The whole envelope must also fit the negotiated frame size.</summary>
    public const int MaximumDataBytes = 256 * 1024;

    /// <summary>DATA frames a sender may send in one direction before it receives a CREDIT frame.</summary>
    public const int InitialCredits = 2;

    public const int MaximumRequestIdLength = 64;
    public const int MaximumErrorCodeLength = 64;

    /// <summary>
    /// Rejects a frame that is malformed on its own. Sequence continuity, credit accounting and the
    /// binding to an open request are receiver state and are not checked here.
    /// </summary>
    public static void Validate(ProxyBodyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.RequestId.Length is < 1 or > MaximumRequestIdLength || frame.Sequence < 0 ||
            frame.Direction is not (ProxyBodyFrame.Types.Direction.Request or ProxyBodyFrame.Types.Direction.Response))
            throw new InvalidDataException("The proxy body frame is invalid.");
        var valid = frame.Operation switch
        {
            ProxyBodyFrame.Types.Operation.Data =>
                frame.Data.Length is >= 1 and <= MaximumDataBytes && frame.ErrorCode.Length == 0,
            ProxyBodyFrame.Types.Operation.End => frame.Data.IsEmpty && frame.ErrorCode.Length == 0,
            ProxyBodyFrame.Types.Operation.Credit =>
                frame.Data.IsEmpty && frame.ErrorCode.Length == 0 && frame.Sequence == 0,
            ProxyBodyFrame.Types.Operation.Abort =>
                frame.Data.IsEmpty && frame.Sequence == 0 &&
                frame.ErrorCode.Length is >= 1 and <= MaximumErrorCodeLength && !frame.ErrorCode.Any(char.IsControl),
            _ => false
        };
        if (!valid) throw new InvalidDataException("The proxy body frame is invalid.");
    }

    /// <summary>A streamed request announces its body with the flag and leaves the inline body empty.</summary>
    public static void ValidateHeader(ProxyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.BodyStreamed && !request.Body.IsEmpty)
            throw new InvalidDataException("A streamed proxy request carries no inline body.");
    }

    /// <summary>A streamed response announces its body with the flag and leaves the inline body empty.</summary>
    public static void ValidateHeader(ProxyResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.BodyStreamed && !response.Body.IsEmpty)
            throw new InvalidDataException("A streamed proxy response carries no inline body.");
    }
}
