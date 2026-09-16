using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Infrastructure.Services;

public sealed class AgreementPdfRenderer : IAgreementPdfRenderer
{
    static AgreementPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseEnvironmentFonts = true;
    }

    public byte[] Render(
        AgreementDocument document,
        bool isCertified,
        Guid? certifyingLawyerId,
        DateTimeOffset? certifiedAt,
        IReadOnlyCollection<SignatureRecord> signatures)
    {
        var terms = JsonSerializer.Deserialize<AgreementTerms>(document.TermsJson)
            ?? throw new InvalidOperationException("Stored agreement terms could not be deserialized.");

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11).LineHeight(1.35f));

                page.Header().Column(c =>
                {
                    c.Item().Text("RENTBRIDGE — TENANCY AGREEMENT")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().PaddingTop(4).Text(
                            $"Draft v{document.Version} · Drafted {document.DraftedAt:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(c =>
                {
                    Section(c, "1. Parties");
                    c.Item().Text($"Landlord: {terms.Landlord.Name} ({terms.Landlord.Email})");
                    if (!string.IsNullOrWhiteSpace(terms.Landlord.Phone))
                        c.Item().Text($"Landlord phone: {terms.Landlord.Phone}");
                    c.Item().Text($"Tenant: {terms.Tenant.Name} ({terms.Tenant.Email})");
                    if (!string.IsNullOrWhiteSpace(terms.Tenant.Phone))
                        c.Item().Text($"Tenant phone: {terms.Tenant.Phone}");
                    if (terms.Lawyer is not null)
                    {
                        c.Item().Text($"Assigned lawyer: {terms.Lawyer.Name} ({terms.Lawyer.Email})");
                        c.Item().Text($"Lawyer role: {terms.Lawyer.Role}");
                    }

                    Section(c, "2. Property");
                    c.Item().Text($"Title: {terms.Property.Title}");
                    if (!string.IsNullOrWhiteSpace(terms.Property.Description))
                        c.Item().Text($"Description: {terms.Property.Description}");
                    c.Item().Text($"Address: {terms.Property.Address}");

                    Section(c, "3. Rent");
                    c.Item().Text($"Rent: {terms.Rent.Currency} {terms.Rent.Amount:N2}");
                    c.Item().Text(
                        "Payment is held in escrow and released only after all " +
                        "verification, inspection, and legal-review conditions are confirmed " +
                        "complete, less the platform commission and legal-fee share.");

                    Section(c, "4. Lease particulars");
                    c.Item().Text($"Lease created: {terms.LeaseCreatedAt:yyyy-MM-dd HH:mm} UTC");
                    if (terms.InspectionScheduledDate is not null)
                        c.Item().Text($"Inspection scheduled: {terms.InspectionScheduledDate:yyyy-MM-dd HH:mm} UTC");

                    if (isCertified && certifyingLawyerId is not null && certifiedAt is not null)
                    {
                        Section(c, "5. Certification");
                        var lawyerName = terms.Lawyer?.Name ?? certifyingLawyerId.ToString();
                        c.Item().Text($"Certified by: {lawyerName}");
                        c.Item().Text($"Certifying lawyer id: {certifyingLawyerId}");
                        c.Item().Text($"Certified at: {certifiedAt:yyyy-MM-dd HH:mm} UTC");
                    }

                    if (signatures.Count > 0)
                    {
                        Section(c, "6. Signatures");
                        foreach (var signature in signatures)
                        {
                            c.Item().PaddingBottom(6).Column(s =>
                            {
                                s.Item().Text($"{signature.Party}: {termsFor(signature.Party)}")
                                    .SemiBold();
                                s.Item().Text($"Signed: {signature.SignedAt:yyyy-MM-dd HH:mm} UTC");
                                s.Item().Text($"IP: {signature.IpAddress}");
                                s.Item().Text($"Signature hash: {Truncate(signature.ImageHash)}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    }

                    Section(c, "7. Read-only");
                    c.Item().Text(
                            "This agreement is immutable once signed. Any change requires a new " +
                            "addendum (higher draft version) referencing this one.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);

                    string termsFor(Domain.Enums.LeaseParty party) => party switch
                    {
                        Domain.Enums.LeaseParty.Landlord => terms.Landlord.Name,
                        Domain.Enums.LeaseParty.Tenant => terms.Tenant.Name,
                        _ => party.ToString(),
                    };
                });

                page.Footer().Column(c =>
                {
                    c.Item().Text($"Content SHA-256: {document.ContentHash}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Text("RentBridge · Lawyer-backed rental marketplace")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return pdf.GeneratePdf();
    }

    private static void Section(ColumnDescriptor c, string title)
    {
        c.Item().PaddingTop(10).Text(title)
            .FontSize(12).SemiBold().FontColor(Colors.Blue.Darken3);
    }

    private static string Truncate(string hash)
        => hash.Length <= 24 ? hash : string.Concat(hash.AsSpan(0, 24), "…");
}