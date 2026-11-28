import { TicketRepository } from "./TicketRepository.js";

// The API's address comes from appsettings.json ("API:BaseUrl"), through the
// controller and the view's data-api-base attribute.
const app = document.querySelector("#ticketApp");
const repo = new TicketRepository(app.dataset.apiBase);

// Role logic
const CAN_SEE_ALL  = app.dataset.canSeeAll === "true";
const CURRENT_USER = app.dataset.currentUserId;
const DETAILS_READY = false; 

// For HTML dialog boxes
const tbody = document.querySelector("#ticketTable tbody");

const createDialog = document.querySelector("#createDialog");
const editDialog   = document.querySelector("#editDialog");
const deleteDialog = document.querySelector("#deleteDialog");

const createForm = document.querySelector("#createTicketForm");
const editForm   = document.querySelector("#editTicketForm");
const deleteForm = document.querySelector("#deleteTicketForm");


loadTickets();

async function loadTickets() {
    try {
        const tickets = CAN_SEE_ALL ? await repo.readAll() : await repo.readMine();
        tbody.innerHTML = "";
        tickets.forEach(t => addTicketRow(t));
    } catch (err) {
        showError(err.message);
    }
}

// Helper functions

function canAccess(ticket) {
    return CAN_SEE_ALL || ticket.userId === CURRENT_USER;
} // End function

function pretty(value) {
    return String(value).replace(/([a-z])([A-Z])/g, "$1 $2");
} // End function

function addTicketRow(t) {
    const row = document.createElement("tr");
    row.dataset.id = t.ticketId;
    const titleCell = DETAILS_READY
        ? `<a href="/Tickets/Details/${t.ticketId}">${escapeHtml(t.title)}</a>`
        : escapeHtml(t.title);
    row.innerHTML = `
        <td>${t.ticketId}</td>
        <td class="ticket-title">${titleCell}</td>
        <td class="ticket-priority">${escapeHtml(pretty(t.priority))}</td>
        <td class="ticket-status">${escapeHtml(pretty(t.status))}</td>
        <td class="ticket-created">${new Date(t.createdAt).toLocaleString()}</td>
        <td class="text-end">
            ${canAccess(t) ? `<button type="button" class="btn btn-sm btn-warning edit-btn" data-id="${t.ticketId}">Edit</button>` : ""}
            ${deleteDialog ? `<button type="button" class="btn btn-sm btn-danger delete-btn" data-id="${t.ticketId}">Delete</button>` : ""}
        </td>`;
    tbody.appendChild(row);
} // End function

function updateTicketRow(id, values) {
    const row = tbody.querySelector(`tr[data-id="${CSS.escape(String(id))}"]`);
    if (!row) return;
    row.querySelector(".ticket-title").textContent = values.Title;
    row.querySelector(".ticket-priority").textContent = pretty(values.Priority);
    if (values.Status) row.querySelector(".ticket-status").textContent = pretty(values.Status);
} // End function


function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
} // End function


function showError(message) {
    const alert = document.querySelector("dialog[open] .dialog-error")
        ?? document.querySelector("#errorAlert");
    alert.textContent = message;
    alert.classList.remove("d-none");
} // End function


// Dialog plumbing

document.querySelectorAll("dialog").forEach(dialog => {
    dialog.querySelectorAll(".close-btn").forEach(button =>
        button.addEventListener("click", () => dialog.close()));

    dialog.addEventListener("close", () => {
        dialog.querySelector("form")?.reset();
        dialog.querySelector(".dialog-error")?.classList.add("d-none");
    });
});

document.querySelector("#openCreateBtn").addEventListener("click", () => {
    createDialog.showModal();
});

// Create: POST api/tickets
createForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const fd = new FormData(createForm);
    try {
        const t = await repo.create({
            title: fd.get("Title"),
            description: fd.get("Description") ?? "",
            priority: fd.get("Priority")
        });
        addTicketRow(t);
        createDialog.close();
    } catch (err) {
        showError(err.message);
    } // End try/catch
});

// Edit part one: GET api/tickets/{id}
document.querySelector("#ticketTable").addEventListener("click", async (e) => {
    const button = e.target.closest(".edit-btn");
    if (!button) return;

    try {
        const t = await repo.read(button.dataset.id); // also re-checks access (403 -> error)
        editForm.elements["Id"].value = t.ticketId;
        editForm.elements["Title"].value = t.title;
        editForm.elements["Description"].value = t.description;
        editForm.elements["Priority"].value = t.priority;
        if (editForm.elements["Status"]) {
            editForm.elements["Status"].value = t.status;
            editForm.elements["OriginalStatus"].value = t.status;
        } // End if
        editDialog.showModal();
    } catch (err) {
        showError(err.message);
    } // End try/catch
});

// Edit part two: PUT api/tickets/{id}
editForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(editForm);
    const id = formData.get("Id");
    const payload = {
        title: formData.get("Title"),
        description: formData.get("Description"),
        priority: formData.get("Priority")
    };

    try {
        await repo.update(id, payload);

        if (editForm.elements["Status"]) {
            const original = formData.get("OriginalStatus");
            const newStatus = formData.get("Status");
            if (newStatus !== original) {
                await repo.updateStatus(id, newStatus);
            } // End if
        } // End if

        updateTicketRow(id, Object.fromEntries(formData));
        editDialog.close();
    } catch (err) {
        showError(err.message);
    } // End try/catch
});

// Delete part one
document.querySelector("#ticketTable").addEventListener("click", (e) => {
    const button = e.target.closest(".delete-btn");
    if (!button || !deleteDialog) return;

    const row = button.closest("tr");
    deleteForm.elements["Id"].value = button.dataset.id;
    document.querySelector("#deleteTicketId").textContent = button.dataset.id;
    document.querySelector("#deleteTicketName").textContent =
        row.querySelector(".ticket-title").textContent;
    deleteDialog.showModal();
});

// Delete part two: DELETE api/tickets/{id}
deleteForm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const id = deleteForm.elements["Id"].value;

    try {
        await repo.delete(id);
        tbody.querySelector(`tr[data-id="${CSS.escape(id)}"]`)?.remove();
        deleteDialog.close();
    } catch (err) {
        showError(err.message);
    } // End try/catch
});