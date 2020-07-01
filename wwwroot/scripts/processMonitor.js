$(function () {
    app.container = $('#dashboard');
    app.initConnection();
    $('#stop').click(app.closeConnection);
    $('#start').click(() => {
        if (!app.socket || app.socket.readyState === app.socket.CLOSED) {
            app.initConnection();
        }
    });
});

const app = {
    state: null,
    container: null,
    socket: null,

    initConnection: function () {
        app.state = new Set();
        app.container.find("tbody").empty();
        app.socket = new WebSocket(`ws://${window.location.host}/api/data/${this.getOrAddConnectionId()}`);
        
        console.log("Server communication started");
        app.socket.onopen = console.log;
        app.socket.onmessage = this.handleServerMessage;
        app.socket.onerror = console.log;
        app.socket.onclose = console.log;
    },

    closeConnection: function () {
        if (app.socket && app.socket.readyState === app.socket.OPEN) {
            app.socket.close();
            console.log("Server communication stopped");
        }
    },

    handleServerMessage: function (message) {
        let data = JSON.parse(message.data);
        switch (data.operation) {
            case 0:
                app.state.has(data.payload.name) ? app.updateRow(data.payload) : app.appendRow(data.payload);
                app.state.add(data.payload.name);
                app.container.find('tbody tr').sortElements(app.rowComparer)
                break;
            case 1:
                app.removeRow(data.payload.name);
                delete app.state[data.name];
            default:
                console.log("Incorrect message format from server")
        }
    },

    updateRow: function (data) {
        for (const [key, value] of Object.entries(data)) {
            $(`[name='${data.name}'] td.${key}`).text(value);
        }
    },

    appendRow: function (data) {
        let row = $('<tr></tr>');
        row.attr("name", data.name);
        for (const [key, value] of Object.entries(data)) {
            row.append($(`<td class='${key}'>${value}</td>`));
        }
        app.container.find('tbody').append(row);
    },

    removeRow: function (processName) {
        app.container.remove(`[name="${processName}"]`);
    },

    getOrAddConnectionId: function() {
        if (!sessionStorage.connectionId) {
            sessionStorage.connectionId = uuid.v4();
        }
        return sessionStorage.connectionId;
    },

    rowComparer: function (a, b) {
        const aCpu = parseFloat($(a).find("td.cpu").text());
        const bCpu = parseFloat($(b).find("td.cpu").text());
        if (aCpu === bCpu) {
            return 0;
        }
        return aCpu > bCpu ? -1 : 1;
    }
}

