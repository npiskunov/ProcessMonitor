$(function () {
    notifications.init();
});

const notifications = {
    init: function () {
        if (!window.Notification) {
            alert("Browser doesnt support notifications");
            return;
        }

        if (Notification.permission === "granted") {
            notifications.watchNotifications();
        }
        else {
            Notification.requestPermission(function (permission) {
                if (permission === "granted") {
                    notifications.watchNotifications();
                }
            });
        }
    },

    watchNotifications: function () {
        $.getJSON(`/api/notifications`, (data) => {
            let type;
            switch (data.type) {
                case 0:
                    type = "CPU";
                    break;
                case 1:
                    type = "Memory";
                    break;
                case 2:
                    type = "Disk";
                    break;
                default:
                    type = "Unknown";
            }
            var notification = new Notification(`Resources alert: ${type} usage exceeds threshold of ${data.threshold}. Actual value is ${data.value}`);
        }).then(notifications.watchNotifications);
    }
}

