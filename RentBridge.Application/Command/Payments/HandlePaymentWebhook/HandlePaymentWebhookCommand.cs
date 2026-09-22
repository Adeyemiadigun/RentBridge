using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Payments;

/// <summary>
/// Internal entry point for a payment-provider webhook. The signature is
/// verified, the matching escrow payment is marked paid, and release is
/// attempted automatically when the landlord payout recipient is on file.
/// </summary>
public sealed record HandlePaymentWebhookCommand(string RawBody, string Signature) : IRequest<Result>;