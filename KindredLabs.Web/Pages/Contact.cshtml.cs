using System.ComponentModel.DataAnnotations;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages;

public class ContactModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ContactModel(
        IEmailService emailService,
        IConfiguration configuration,
        IStringLocalizer<Resources.Pages.Contact> localizer
    )
    {
        _emailService = emailService;
        _configuration = configuration;
        Localizer = localizer;
    }

    public IStringLocalizer<Resources.Pages.Contact> Localizer { get; }

    [BindProperty]
    [Required(ErrorMessage = "NameRequired")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "SubjectRequired")]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "CategoryRequired")]
    public string Category { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "MessageRequired")]
    [MinLength(10, ErrorMessage = "MessageMinLength")]
    public string Message { get; set; } = string.Empty;

    [BindProperty]
    public string? Website { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = Localizer["PageTitle"];
        ViewData["Description"] = Localizer["PageDescription"];
        ViewData["CanonicalUrl"] = Localizer["CanonicalUrl"];
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Honeypot check
        if (!string.IsNullOrEmpty(Website))
        {
            TempData["ContactSuccess"] = true;
            return RedirectToPage();
        }

        var destinationEmail = Category switch
        {
            "General" => _configuration["Contact:GeneralEmail"],
            "Partnerships" => _configuration["Contact:PartnershipsEmail"],
            "Media" => _configuration["Contact:MediaEmail"],
            _ => _configuration["Contact:GeneralEmail"],
        };

        if (string.IsNullOrEmpty(destinationEmail))
        {
            destinationEmail = "contact@kindredlabsfoundation.org";
        }

        var categoryLabel = Category switch
        {
            "General" => Localizer["CategoryGeneral"].Value,
            "Partnerships" => Localizer["CategoryPartnerships"].Value,
            "Media" => Localizer["CategoryMedia"].Value,
            _ => Category,
        };

        await _emailService.SendContactRequestAsync(
            destinationEmail,
            Email,
            Name,
            Subject,
            categoryLabel,
            Message
        );
        await _emailService.SendContactConfirmationAsync(Email);

        TempData["ContactSuccess"] = true;
        return RedirectToPage();
    }
}
