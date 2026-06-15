document.addEventListener('DOMContentLoaded', function () {
    const emailInput = document.getElementById('EmailInput');
    const confirmEmailContainer = document.getElementById('ConfirmEmailContainer');
    const confirmEmailInput = confirmEmailContainer.querySelector('input');
    const prefilledEmail = emailInput.getAttribute('data-prefilled');
    const isAuthenticated = prefilledEmail !== '';

    function toggleConfirmEmail() {
        if (!isAuthenticated) {
            confirmEmailContainer.style.display = 'block';
            confirmEmailInput.required = true;
        } else {
            if (emailInput.value.toLowerCase() !== prefilledEmail.toLowerCase()) {
                confirmEmailContainer.style.display = 'block';
                confirmEmailInput.required = true;
            } else {
                confirmEmailContainer.style.display = 'none';
                confirmEmailInput.required = false;
                // Optionally clear confirmEmailInput value when hidden
                if (confirmEmailContainer.style.display === 'none') {
                    // confirmEmailInput.value = '';
                }
            }
        }
    }

    emailInput.addEventListener('input', toggleConfirmEmail);
    toggleConfirmEmail(); // Initial state
});
