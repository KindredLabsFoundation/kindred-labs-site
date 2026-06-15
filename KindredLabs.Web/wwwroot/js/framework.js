(function () {
    const trigger = document.getElementById('forms-dropdown-trigger');
    const menu = document.getElementById('forms-dropdown-menu');
    const container = document.getElementById('forms-dropdown-container');

    if (!trigger || !menu || !container) return;

    trigger.addEventListener('click', function (e) {
        e.preventDefault();
        menu.classList.toggle('hidden');
    });

    document.addEventListener('click', function (e) {
        if (!container.contains(e.target)) {
            menu.classList.add('hidden');
        }
    });
})();
