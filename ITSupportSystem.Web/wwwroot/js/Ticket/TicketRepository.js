export class TicketRepository {
    #baseAddress

    constructor(baseAddress) {
        this.#baseAddress = baseAddress.replace(/\/+$/, "");
    } // End constructor

    async #send(path = "", options = {}) {
        const response = await fetch(`${this.#baseAddress}${path}`, {
            headers: { "Content-Type": "application/json" },
            ...options
        });
        if (!response.ok) {
            const detail = await response.text();
            throw new Error(`HTTP ${response.status} ${response.statusText}: ${detail}`);
        } // End if

        if (response.status === 204) return null;
        return response.json();
    } // End async

    // CRUD logic here

    // Read all method 
    readAll() {
        return this.#send("/api/tickets");
    } // End method

    // Read mine method
    readMine() {
        return this.#send("/api/tickets/mine");
    } // End method

    // Read (one) method
    read(id) {
        return this.#send(`/api/tickets/${encodeURIComponent(id)}`);
    } // End method

    // Create method
    create(ticket) {
        return this.#send("/api/tickets", { method: "POST", body: JSON.stringify(ticket) });
    } // End method

    // Update method
    update(id, ticket) {
        return this.#send(`/api/tickets/${encodeURIComponent(id)}`,
                { method: "PUT", body: JSON.stringify(ticket) });
    } // End method

    // Update status method
    updateStatus(id, status) {
        return this.#send(`/api/tickets/${encodeURIComponent(id)}/status`,
                { method: "PUT", body: JSON.stringify({ status }) });
    } // End method

    // Delete method 
    delete(id) {
        return this.#send(`/api/tickets/${encodeURIComponent(id)}`,
                    { method: "DELETE" });
    } // End method
} // End export