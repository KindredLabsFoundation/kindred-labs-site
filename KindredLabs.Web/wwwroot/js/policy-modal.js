/**
 * Policy Modal JavaScript
 * Handles scroll-to-accept logic for Privacy Policy and Terms of Service modals.
 */

(function () {
    // Global storage for acceptance state
    window.policyAcceptance = {
        PrivacyPolicy: false,
        TermsOfService: false
    };

    /**
     * Opens a policy modal.
     * @param {string} policyType - "PrivacyPolicy" or "TermsOfService"
     * @param {string} checkboxId - ID of the checkbox to check on accept
     * @param {string} pageUrl - URL of the policy page to load in iframe
     */
    window.openPolicyModal = function (policyType, checkboxId, pageUrl) {
        const modal = document.getElementById(`policy-modal-${policyType}`);
        if (!modal) return;

        // Reset state
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden'; // Prevent background scrolling

        const iframe = modal.querySelector('.policy-iframe');
        
        // Pass the checkboxId to the iframe via name attribute or custom data
        // Actually, we'll just ensure window.acceptPolicy is available to the iframe.
    };

    /**
     * Accepts a policy and closes the modal.
     * Called from within the iframe.
     * @param {string} policyType - "PrivacyPolicy" or "TermsOfService"
     * @param {string} checkboxId - ID of the checkbox to check on accept
     */
    window.acceptPolicy = function (policyType, checkboxId) {
        // Mark as accepted
        window.policyAcceptance[policyType] = true;

        // Check the target checkbox
        const checkbox = document.getElementById(checkboxId);
        if (checkbox) {
            checkbox.checked = true;
            // Trigger change event if needed for other scripts
            checkbox.dispatchEvent(new Event('change', { bubbles: true }));
        }

        // Close modal
        closePolicyModal(`policy-modal-${policyType}`);
    };

    /**
     * Closes a policy modal.
     * @param {string} modalId - ID of the modal element
     */
    function closePolicyModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('hidden');
            document.body.style.overflow = ''; // Restore scrolling
        }
    }

    // Initialize event listeners when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        // Handle Close button clicks
        document.querySelectorAll('.policy-modal-close').forEach(btn => {
            btn.addEventListener('click', function () {
                closePolicyModal(this.getAttribute('data-modal-id'));
            });
        });


        // Close modal on background click
        document.querySelectorAll('.policy-modal').forEach(modal => {
            modal.addEventListener('click', function (e) {
                if (e.target === this) {
                    closePolicyModal(this.id);
                }
            });
        });

        // Handle Policy Trigger clicks
        document.querySelectorAll('[data-policy-trigger]').forEach(trigger => {
            trigger.addEventListener('click', function (e) {
                e.preventDefault();
                const policyType = this.getAttribute('data-policy-trigger');
                const checkboxId = policyType === 'PrivacyPolicy' ? 'Input_AcceptedPrivacyPolicy' : 'Input_AcceptedTermsOfService';
                const currentCulture = window.location.pathname.split('/')[1] || 'en';
                const pageUrl = `/${currentCulture}/${policyType === 'PrivacyPolicy' ? 'Privacy' : 'Terms'}`;
                
                window.openPolicyModal(policyType, checkboxId, pageUrl);
            });
        });
    });
})();
