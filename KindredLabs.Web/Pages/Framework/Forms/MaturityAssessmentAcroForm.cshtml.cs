using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KindredLabs.Web.Pages.Framework.Forms;

public class MaturityAssessmentAcroFormModel : PageModel
{
    private readonly IPdfService _pdfService;

    public MaturityAssessmentAcroFormModel(IPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var culture = HttpContext.Items["culture"]?.ToString() ?? "en";
        var pdfBytes = await _pdfService.GenerateAcroFormPdfAsync(FormType.MaturityAssessment, culture);
        
        return File(pdfBytes, "application/pdf", $"MaturityAssessment_Blank_{culture}.pdf");
    }
}
