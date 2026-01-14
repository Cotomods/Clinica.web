// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    var toggle = document.getElementById("sidebarToggle");
    if (toggle) {
        toggle.addEventListener("click", function () {
            if (document.body.classList.contains("sidebar-expanded")) {
                document.body.classList.remove("sidebar-expanded");
                document.body.classList.add("sidebar-collapsed");
            } else {
                document.body.classList.remove("sidebar-collapsed");
                document.body.classList.add("sidebar-expanded");
            }
        });
    }

    var darkToggle = document.getElementById("darkModeToggle");
    if (darkToggle) {
        // Inicializar desde localStorage
        var darkEnabled = localStorage.getItem("darkMode") === "true";
        if (darkEnabled) {
            document.body.classList.add("dark-mode");
        }

        darkToggle.addEventListener("click", function () {
            document.body.classList.toggle("dark-mode");
            var enabled = document.body.classList.contains("dark-mode");
            localStorage.setItem("darkMode", enabled ? "true" : "false");
        });
    }
});
