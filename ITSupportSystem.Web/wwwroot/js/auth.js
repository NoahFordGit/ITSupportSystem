const tabs = document.querySelectorAll(".auth-tabs .tab");
const forms = document.querySelectorAll("#login-form, #register-form");

tabs.forEach(tab => {
    tab.addEventListener("click", () => {
        const target = tab.dataset.target;

        tabs.forEach(t => t.classList.remove("active"));
        tab.classList.add("active");

        forms.forEach(form => {
            form.hidden = form.id !== target;
        });
    });
})