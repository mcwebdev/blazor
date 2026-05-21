document.addEventListener('click', function (e) {
    var dropdowns = document.querySelectorAll('.dropdown-menu.show');
    dropdowns.forEach(function (d) {
        var container = d.closest('.dropdown-container');
        if (container && !container.contains(e.target)) {
            d.classList.remove('show');
        }
    });
});

window.toggleDropdown = function (event, button) {
    event.preventDefault();
    event.stopPropagation();
    var container = button.closest('.dropdown-container');
    var dropdown = container.querySelector('.dropdown-menu');
    
    // Close others
    document.querySelectorAll('.dropdown-menu.show').forEach(function(d) {
        if (d !== dropdown) d.classList.remove('show');
    });
    
    if (dropdown) {
        dropdown.classList.toggle('show');
    }
};
