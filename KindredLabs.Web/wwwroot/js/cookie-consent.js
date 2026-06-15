document.addEventListener('DOMContentLoaded', function () {
    const banner = document.getElementById('cookie-consent-banner');
    const dismissBtn = document.getElementById('cookie-consent-dismiss');

    if (banner && dismissBtn) {
        dismissBtn.addEventListener('click', function () {
            // Set cookie: CookieConsentGiven=true; path=/; max-age=31536000 (1 year)
            document.cookie = "CookieConsentGiven=true; path=/; max-age=31536000; SameSite=Lax";
            
            // Hide banner
            banner.classList.add('translate-y-full');
            setTimeout(() => {
                banner.remove();
            }, 500);
        });
    }
});
