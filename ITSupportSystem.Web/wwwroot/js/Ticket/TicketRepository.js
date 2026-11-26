export class TicketRepository {
    #baseAddress

    constructor(baseAddress) {
        this.#baseAddress = baseAddress;
    } // End constructor


    async #send(path = "", options = {}) {
        const response = await fetch(`${this.#baseAddress}${path}`, options);
        if (!response.ok) {
            const detail = await response.text();
            throw new Error(`HTTP ${response.status} ${response.statusText}: ${detail}`);
        } // End if

        if (response.status === 204) return null;
        return response.json();
    } // End async

    // CRRUD logic here

    // Read all method
    readAll(){
        return this.#send();
    } // End method


    // Read (one) method
    read(id) {
        return this.#send(`$/${encodeURIComponent(id)}`);
    } // End method


    // Create method
    create(formData) {
        return this.#send("", { method: "POST", body: formData});
    } // End method 


    // Update method
    update(id, formData) {
        return this.#send(`/${encodeURIComponent(id)}`,
                { method: "PUT", body: formData });
    } // End method


    // Delete method
    delete(id) {
        return this.#send(`/${encodeURIComponent(id)}`,
                    { method: "DELETE"});
    } // End method

} // End export