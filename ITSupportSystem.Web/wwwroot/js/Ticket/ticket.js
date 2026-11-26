import { TicketRepository } from "./TicketRepository";

const repo = new TicketRepository("/api/tickets");
// Put other const variables related to the view here

const tbody = document.querySelector("#ticketTable tbody");

loadTickets();

async function loadTickets() {
    try {
        const tickets = await repo.readAll();
        tbody.innerHTML = "";
        tickets.array.forEach(ticket => addTicketRow(ticket));
    } catch (err) {
        showError(err.message);
    } // End try catch
} // End function


function addTicketRow(ticket) {
    const row = document.createElement("tr");
    row.dataset.id = ticket.id;
    row.innerHTML = `
    <td>${ticket.id}</td>
        <td class="game-title">${escapeHtml(ticket.title)}</td>
        <td class="game-platform">${escapeHtml(String(game.platform))}</td>
        <td class="game-releaseYear">${escapeHtml(Number(game.releaseYear))}</td>
        <td class="game-price">${escapeHtml(Number(game.price))}</td>
        <td class="text-end">
            <button type="button" class="btn btn-sm btn-info details-btn" data-id="${game.id}">Details</button>
            <button type="button" class="btn btn-sm btn-warning edit-btn" data-id="${game.id}">Edit</button>
            <button type="button" class="btn btn-sm btn-danger delete-btn" data-id="${game.id}">Delete</button>
        </td>`;
    
} // End function