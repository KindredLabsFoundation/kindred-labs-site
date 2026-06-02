function confirmDelete(formId, message) {
    if (confirm(message)) {
        document.getElementById(formId).submit();
    }
}

function clearForm(formId) {
    document.getElementById(formId)?.dispatchEvent(new CustomEvent('beforeFormClear'));
    const form = document.getElementById(formId);
    if (!form) return;

    form.querySelectorAll('input:not([data-no-clear]):not([type="hidden"]):not([type="submit"])').forEach(el => {
        if (el.type === 'radio' || el.type === 'checkbox') {
            el.checked = false;
        } else {
            el.value = '';
        }
    });

    form.querySelectorAll('textarea:not([data-no-clear])').forEach(el => {
        el.value = '';
    });

    form.querySelectorAll('select:not([data-no-clear])').forEach(el => {
        el.selectedIndex = 0;
    });

    // Reset dirty flag
    if (typeof isDirty !== 'undefined') {
        isDirty = false;
    }

    document.getElementById(formId)?.dispatchEvent(new CustomEvent('formCleared'));
}
