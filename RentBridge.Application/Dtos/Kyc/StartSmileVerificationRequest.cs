namespace RentBridge.Application.Dtos.Kyc
{
    public sealed record StartSmileVerificationRequest
    {
        public string Nin { get; init; } = string.Empty;
    }
}