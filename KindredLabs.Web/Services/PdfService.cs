using System.Text.Json;
using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Services;

/// <summary>
/// Implementation of <see cref="IPdfService"/> using iText7 for PDF generation.
/// </summary>
public class PdfService : IPdfService
{
    private readonly IStringLocalizer<PdfService> _localizer;
    private static readonly Color BackgroundColor = new DeviceRgb(0x16, 0x21, 0x3E);
    private static readonly Color PrimaryTextColor = new DeviceRgb(0xF0, 0xF0, 0xF0);
    private static readonly Color AccentColor = new DeviceRgb(0xFF, 0xD7, 0x00);

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfService"/> class.
    /// </summary>
    /// <param name="localizer">The string localizer for PDF content.</param>
    public PdfService(IStringLocalizer<PdfService> localizer)
    {
        _localizer = localizer;
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateSubmissionPdfAsync(
        FormType formType,
        string formDataJson,
        Guid submissionId,
        string contentHash,
        string verifiedEmail,
        DateTime submittedAt,
        string signatureBase64
    )
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        // Apply brand background color to current and future pages
        pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new BackgroundEventHandler(BackgroundColor));

        // Header Info
        document.Add(
            new Paragraph($"{_localizer["SubmissionId"]}: {submissionId}").SetFontColor(AccentColor)
        );
        document.Add(new Paragraph($"{_localizer["ContentHash"]}: {contentHash}").SetFontSize(10));
        document.Add(
            new Paragraph($"{_localizer["VerifiedEmail"]}: {verifiedEmail}").SetFontSize(10)
        );
        document.Add(
            new Paragraph(
                $"{_localizer["Timestamp"]}: {submittedAt:yyyy-MM-dd HH:mm:ss} UTC"
            ).SetFontSize(10)
        );

        document.Add(new AreaBreak());

        document.Add(
            new Paragraph(formType.ToString())
                .SetFontSize(24)
                .SetFontColor(PrimaryTextColor)
                .SetTextAlignment(TextAlignment.CENTER)
        );

        // Form Data
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(formDataJson, options);
        if (data != null)
        {
            foreach (var kvp in data)
            {
                document.Add(new Paragraph($"{kvp.Key}: {kvp.Value}").SetFontSize(12));
            }
        }

        // Signature
        if (!string.IsNullOrEmpty(signatureBase64))
        {
            document.Add(new Paragraph(_localizer["Signature"]).SetMarginTop(20));
            try
            {
                var cleanBase64 = signatureBase64.Contains(",")
                    ? signatureBase64.Split(',')[1]
                    : signatureBase64;
                var imageData = ImageDataFactory.Create(Convert.FromBase64String(cleanBase64));
                var image = new Image(imageData).SetMaxWidth(200);
                document.Add(image);
            }
            catch
            {
                document.Add(new Paragraph("[Signature Error]"));
            }
        }

        document.Close();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Event handler to draw the background color on every page.
    /// </summary>
    private class BackgroundEventHandler : iText.Kernel.Pdf.Event.AbstractPdfDocumentEventHandler
    {
        private readonly Color _backgroundColor;

        public BackgroundEventHandler(Color backgroundColor)
        {
            _backgroundColor = backgroundColor;
        }

        protected override void OnAcceptedEvent(
            iText.Kernel.Pdf.Event.AbstractPdfDocumentEvent @event
        )
        {
            var docEvent = (iText.Kernel.Pdf.Event.PdfDocumentEvent)@event;
            var pdfDoc = docEvent.GetDocument();
            var page = docEvent.GetPage();
            var canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);
            var rect = page.GetPageSize();

            canvas
                .SaveState()
                .SetFillColor(_backgroundColor)
                .Rectangle(rect.GetLeft(), rect.GetBottom(), rect.GetWidth(), rect.GetHeight())
                .Fill()
                .RestoreState();
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateAcroFormPdfAsync(FormType formType, string locale)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        document.Add(
            new Paragraph($"{formType} - Blank Form ({locale})")
                .SetFontSize(18)
                .SetFontColor(BackgroundColor)
        );

        PdfAcroForm form = PdfAcroForm.GetAcroForm(pdf, true);

        // Use builder for iText 9
        var nameField = new TextFormFieldBuilder(pdf, "name")
            .SetWidgetRectangle(new iText.Kernel.Geom.Rectangle(50, 700, 200, 20))
            .CreateText();

        form.AddField(nameField);
        document.Add(new Paragraph(_localizer["NameLabel"]).SetFixedPosition(50, 720, 200));

        if (formType == FormType.MaturityAssessment)
        {
            // Add scoring JS
            pdf.GetCatalog()
                .SetOpenAction(
                    PdfAction.CreateJavaScript("app.alert('Scoring logic initialized');")
                );
        }

        document.Close();
        return memoryStream.ToArray();
    }
}
