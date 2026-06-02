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
using iText.Layout.Font;
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
    private readonly IStringLocalizer<Resources.Services.PdfService> _localizer;
    private static readonly Color BackgroundColor = ColorConstants.WHITE;
    private static readonly Color PrimaryTextColor = ColorConstants.BLACK;
    private static readonly Color AccentColor = new DeviceRgb(0xFF, 0xD7, 0x00);
    private static readonly ImageData LogoImageData = ImageDataFactory.Create(
        System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "wwwroot",
            "images",
            "Kindred_Labs_Logo_256.png"
        )
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfService"/> class.
    /// </summary>
    /// <param name="localizer">The string localizer for PDF content.</param>
    public PdfService(IStringLocalizer<Resources.Services.PdfService> localizer)
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

        // Apply footer event handler to current and future pages
        pdf.AddEventHandler(
            PdfDocumentEvent.END_PAGE,
            new FooterEventHandler(LogoImageData, AccentColor, PrimaryTextColor)
        );

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

        document.Add(new Paragraph("\n"));

        document.Add(
            new Paragraph(formType.ToString())
                .SetFontSize(24)
                .SetFontColor(PrimaryTextColor)
                .SetTextAlignment(TextAlignment.CENTER)
        );

        if (formType == FormType.MaturityAssessment)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data =
                JsonSerializer.Deserialize<KindredLabs.Web.Pages.Framework.Forms.MaturityAssessmentModel.InputModel>(
                    formDataJson,
                    options
                );
            if (data != null)
            {
                RenderMaturityAssessment(document, data);
            }
        }
        else
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(
                formDataJson,
                options
            );
            if (data != null)
            {
                // Better rendering for all forms
                if (data.TryGetValue("RecordId", out var rid))
                    document.Add(new Paragraph($"{_localizer["RecordId"]}: {rid}").SetFontSize(12));
                if (data.TryGetValue("InternalReference", out var ir))
                    document.Add(
                        new Paragraph($"{_localizer["InternalReferenceLabel"]}: {ir}").SetFontSize(
                            12
                        )
                    );
                if (data.TryGetValue("Date", out var d))
                    document.Add(new Paragraph($"{_localizer["Date"]}: {d}").SetFontSize(12));

                document.Add(new Paragraph("\n"));

                foreach (var kvp in data)
                {
                    if (
                        kvp.Key == "RecordId"
                        || kvp.Key == "InternalReference"
                        || kvp.Key == "Date"
                        || kvp.Key == "SignatureData"
                    )
                        continue;

                    if (
                        kvp.Value is JsonElement element
                        && element.ValueKind == JsonValueKind.Array
                    )
                    {
                        document.Add(
                            new Paragraph($"{kvp.Key}:").SetFontSize(12).SetFontColor(AccentColor)
                        );
                        foreach (var item in element.EnumerateArray())
                        {
                            document.Add(
                                new Paragraph($" - {item.ToString()}")
                                    .SetFontSize(10)
                                    .SetMarginLeft(20)
                            );
                        }
                    }
                    else
                    {
                        document.Add(new Paragraph($"{kvp.Key}: {kvp.Value}").SetFontSize(12));
                    }
                }
            }
        }

        // Signature
        if (!string.IsNullOrEmpty(signatureBase64))
        {
            document.Add(new AreaBreak());
            document.Add(new Paragraph(_localizer["Signature"]).SetFontSize(14).SetMarginTop(20));
            try
            {
                var cleanBase64 = signatureBase64.Contains(",")
                    ? signatureBase64.Split(',')[1]
                    : signatureBase64;
                var imageData = ImageDataFactory.Create(Convert.FromBase64String(cleanBase64));
                var image = new Image(imageData).SetMaxWidth(250);
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

    private void RenderMaturityAssessment(
        Document document,
        KindredLabs.Web.Pages.Framework.Forms.MaturityAssessmentModel.InputModel data
    )
    {
        document.Add(new Paragraph(_localizer["RecordHeader"]).SetFontSize(16).SetMarginTop(10));
        document.Add(new Paragraph($"{_localizer["RecordId"]}: {data.RecordId}"));
        document.Add(new Paragraph($"{_localizer["AssessmentDate"]}: {data.Date:d}"));
        document.Add(new Paragraph($"{_localizer["OrganizationName"]}: {data.OrganizationName}"));
        document.Add(new Paragraph($"{_localizer["AssessmentType"]}: {data.AssessmentType}"));
        document.Add(new Paragraph($"{_localizer["AssessorName"]}: {data.AssessorName}"));
        document.Add(new Paragraph($"{_localizer["AssessorRole"]}: {data.AssessorRole}"));
        document.Add(
            new Paragraph($"{_localizer["AssessorOrganization"]}: {data.AssessorOrganization}")
        );

        foreach (var layer in data.Layers)
        {
            document.Add(new AreaBreak());
            document.Add(
                new Paragraph(layer.Name).SetFontSize(18).SetFontColor(AccentColor).SetMarginTop(10)
            );
            document.Add(new Paragraph($"{_localizer["LayerScore"]}: {layer.LayerScore}"));

            foreach (var domain in layer.Domains)
            {
                document.Add(
                    new Paragraph($"{domain.Id} {domain.Name}").SetFontSize(14).SetMarginTop(10)
                );
                document.Add(new Paragraph($"{_localizer["DomainScore"]}: {domain.DomainScore}"));

                if (domain.IsNotApplicable)
                {
                    document.Add(
                        new Paragraph(_localizer["NotApplicable"]).SetFontColor(ColorConstants.GRAY)
                    );
                    document.Add(
                        new Paragraph($"{_localizer["NADocumentation"]}: {domain.NaDocumentation}")
                    );
                }
                else
                {
                    foreach (var level in domain.Levels)
                    {
                        var answer =
                            level.Answer == true ? "YES" : (level.Answer == false ? "NO" : "—");
                        document.Add(
                            new Paragraph($"Level {level.Level}: {answer}").SetFontSize(10)
                        );
                    }
                }
            }
        }

        document.Add(new AreaBreak());
        document.Add(
            new Paragraph(_localizer["MaturityProfileSummary"]).SetFontSize(16).SetMarginTop(10)
        );
        Table table = new Table(
            UnitValue.CreatePercentArray(new float[] { 30, 50, 20 })
        ).UseAllAvailableWidth();
        table.AddHeaderCell(_localizer["Layer"]);
        table.AddHeaderCell(_localizer["DomainScores"]);
        table.AddHeaderCell(_localizer["LayerScore"]);

        foreach (var layer in data.Layers)
        {
            table.AddCell(layer.Name);
            var domainScores = string.Join(
                "  ",
                layer.Domains.Select(d => $"{d.Id}: {(d.IsNotApplicable ? "N/A" : d.DomainScore)}")
            );
            table.AddCell(domainScores);
            table.AddCell(layer.LayerScore ?? "—");
        }
        document.Add(table);

        document.Add(new Paragraph("\n"));
        document.Add(new Paragraph(_localizer["GapNarrative"]).SetFontSize(16));
        document.Add(new Paragraph($"{_localizer["Layer1GapNarrativeLabel"]}:").SetFontSize(10));
        document.Add(new Paragraph(data.Layer1GapNarrative).SetFontSize(10).SetMarginBottom(10));
        document.Add(new Paragraph($"{_localizer["Layer2GapNarrativeLabel"]}:").SetFontSize(10));
        document.Add(new Paragraph(data.Layer2GapNarrative).SetFontSize(10).SetMarginBottom(10));
        document.Add(new Paragraph($"{_localizer["Layer3GapNarrativeLabel"]}:").SetFontSize(10));
        document.Add(new Paragraph(data.Layer3GapNarrative).SetFontSize(10).SetMarginBottom(10));
    }

    /// <summary>
    /// Event handler to draw the footer on every page.
    /// </summary>
    private class FooterEventHandler : iText.Kernel.Pdf.Event.AbstractPdfDocumentEventHandler
    {
        private readonly ImageData _logoData;
        private readonly Color _accentColor;
        private readonly Color _textColor;

        public FooterEventHandler(ImageData logoData, Color accentColor, Color textColor)
        {
            _logoData = logoData;
            _accentColor = accentColor;
            _textColor = textColor;
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

            float margin = 36;
            float footerHeight = 50;
            float y = rect.GetBottom() + margin;

            canvas
                .SaveState()
                .SetStrokeColor(_accentColor)
                .SetLineWidth(1)
                .MoveTo(rect.GetLeft() + margin, y + footerHeight)
                .LineTo(rect.GetRight() - margin, y + footerHeight)
                .Stroke()
                .RestoreState();

            // Logo on the left
            var logo = new Image(_logoData).ScaleToFit(1000, 40);
            float logoY = y + (footerHeight - 40) / 2;
            new Canvas(canvas, rect).ShowTextAligned(
                new Paragraph().Add(logo),
                rect.GetLeft() + margin,
                logoY,
                TextAlignment.LEFT
            );

            // Page number on the right
            int pageNumber = pdfDoc.GetPageNumber(page);
            new Canvas(canvas, rect).ShowTextAligned(
                new Paragraph(pageNumber.ToString()).SetFontColor(_textColor),
                rect.GetRight() - margin,
                logoY + 15,
                TextAlignment.RIGHT
            );
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateAcroFormPdfAsync(FormType formType, string locale)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        pdf.AddEventHandler(
            PdfDocumentEvent.END_PAGE,
            new FooterEventHandler(LogoImageData, AccentColor, PrimaryTextColor)
        );

        if (formType == FormType.MaturityAssessment)
        {
            GenerateMaturityAssessmentAcroForm(pdf, document, locale);
        }
        else
        {
            document.Add(new Paragraph($"{formType} - Blank Form ({locale})").SetFontSize(18));
        }

        document.Close();
        return memoryStream.ToArray();
    }

    private void GenerateMaturityAssessmentAcroForm(
        PdfDocument pdf,
        Document document,
        string locale
    )
    {
        document.Add(
            new Paragraph(_localizer["PageTitle"])
                .SetFontSize(24)
                .SetTextAlignment(TextAlignment.CENTER)
        );

        PdfAcroForm form = PdfAcroForm.GetAcroForm(pdf, true);

        // Header
        float y = 720;
        AddAcroField(form, document, _localizer["RecordId"], "RecordId", 50, y);
        AddAcroField(form, document, _localizer["AssessmentDate"], "Date", 300, y);
        y -= 40;
        AddAcroField(
            form,
            document,
            _localizer["OrganizationName"],
            "OrganizationName",
            50,
            y,
            450
        );
        y -= 40;
        AddAcroField(form, document, _localizer["AssessorName"], "AssessorName", 50, y);
        AddAcroField(form, document, _localizer["AssessorRole"], "AssessorRole", 300, y);
        y -= 40;
        AddAcroField(
            form,
            document,
            _localizer["AssessorOrganization"],
            "AssessorOrganization",
            50,
            y,
            450
        );

        // JS for Scoring
        string js =
            @"
            function calculate() {
                // Initialize all domain and layer scores to 0
                for (var i = 1; i <= 3; i++) {
                    for (var j = 1; j <= 4; j++) {
                        var domainId = i + '.' + j;
                        var domainField = this.getField('DomainScore_' + domainId);
                        if (domainField && domainField.value == '') {
                            domainField.value = '0';
                        }
                    }
                    var layerField = this.getField('LayerScore_Layer' + i);
                    if (layerField && layerField.value == '') {
                        layerField.value = '0';
                    }
                }
            }
            calculate();
        ";
        pdf.GetCatalog().SetOpenAction(PdfAction.CreateJavaScript(js));
    }

    private void AddAcroField(
        PdfAcroForm form,
        Document document,
        string label,
        string name,
        float x,
        float y,
        float width = 200
    )
    {
        document.Add(new Paragraph(label).SetFixedPosition(x, y + 20, width).SetFontSize(10));
        var field = new TextFormFieldBuilder(form.GetPdfDocument(), name)
            .SetWidgetRectangle(new iText.Kernel.Geom.Rectangle(x, y, width, 18))
            .CreateText();
        form.AddField(field);
    }
}
