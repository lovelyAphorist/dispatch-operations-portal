(() => {
    const config = window.dispatchPortalConfig;
    const DateTime = luxon.DateTime;

    const dateFormatter = (cell) => {
        const value = cell.getValue();
        if (!value) return "";
        const parsed = DateTime.fromISO(value);
        return parsed.isValid ? parsed.toFormat("MM/dd/yyyy h:mm a") : value;
    };

    const table = new Tabulator("#dispatch-table", {
        layout: "fitDataStretch",
        height: "620px",
        pagination: true,
        paginationMode: "remote",
        filterMode: "remote",
        paginationSize: 10,
        paginationSizeSelector: [10, 20, 50],
        ajaxURL: config.dataUrl,
        ajaxRequestFunc: (url, requestConfig, params) => {
            const query = new URLSearchParams({
                page: params.page || 1,
                pageSize: params.size || 10,
                demoUserId: config.demoUserId,
                search: document.querySelector("#search").value || "",
                fromDate: document.querySelector("#fromDate").value || "",
                toDate: document.querySelector("#toDate").value || "",
                filters: JSON.stringify(params.filter || [])
            });
            return fetch(`${url}?${query.toString()}`).then(response => {
                if (!response.ok) throw new Error("Unable to load dispatches.");
                return response.json();
            });
        },
        columns: [
            { title: "Reference", field: "referenceNumber", headerFilter: "input", minWidth: 145 },
            { title: "First Name", field: "firstName", headerFilter: "input" },
            { title: "Last Name", field: "lastName", headerFilter: "input" },
            { title: "Cancellation", field: "cancellationType", headerFilter: "input" },
            { title: "Received", field: "receivedDateTime", formatter: dateFormatter, minWidth: 170 },
            { title: "Dispatched", field: "dispatchDateTime", formatter: dateFormatter, minWidth: 170 },
            { title: "Cleared", field: "clearedFromOnSceneDateTime", formatter: dateFormatter, minWidth: 170 },
            { title: "Clinician Team", field: "respondingClinicianTeam", headerFilter: "input", formatter: cell => cell.getValue() || "N/A", minWidth: 160 },
            { title: "Disposition", field: "disposition", headerFilter: "input", minWidth: 160 },
            { title: "Provider", field: "providerName", minWidth: 165 },
            {
                title: "Actions",
                field: "id",
                headerSort: false,
                minWidth: 225,
                formatter: (cell) => {
                    const row = cell.getRow().getData();
                    const edit = `${config.editUrl}?dispatchId=${row.id}&demoUserId=${config.demoUserId}`;
                    return `
                        <div class="btn-group btn-group-sm" role="group">
                            <a class="btn btn-outline-primary" href="${edit}">Edit</a>
                            <button class="btn btn-outline-secondary history-btn" data-id="${row.id}" data-reference="${row.referenceNumber}">History</button>
                            <button class="btn btn-outline-danger delete-btn" data-id="${row.id}" data-reference="${row.referenceNumber}">Delete</button>
                        </div>`;
                },
                cellClick: (event) => event.stopPropagation()
            }
        ]
    });

    document.querySelector("#applyFilters").addEventListener("click", () => table.setData());
    document.querySelector("#search").addEventListener("keydown", (event) => {
        if (event.key === "Enter") table.setData();
    });
    document.querySelector("#demoUser").addEventListener("change", (event) => {
        window.location.href = `${config.dashboardUrl}?demoUserId=${encodeURIComponent(event.target.value)}`;
    });

    const historyModal = new bootstrap.Modal(document.querySelector("#historyModal"));
    let historyTable;

    document.querySelector("#dispatch-table").addEventListener("click", async (event) => {
        const historyButton = event.target.closest(".history-btn");
        if (historyButton) {
            const id = historyButton.dataset.id;
            document.querySelector("#historyTitle").textContent = `${historyButton.dataset.reference} history`;
            const response = await fetch(`${config.historyUrl}?dispatchId=${id}&demoUserId=${config.demoUserId}`);
            const history = await response.json();

            const rows = Array.isArray(history) ? history : [];
            if (historyTable) {
                historyTable.replaceData(rows);
            } else {
                historyTable = new Tabulator("#history-table", {
                    data: rows,
                    layout: "fitColumns",
                    placeholder: "No audit history has been recorded yet.",
                    columns: [
                        { title: "Changed", field: "changedDate", formatter: dateFormatter, width: 180 },
                        { title: "Changed By", field: "changedBy", width: 180 },
                        { title: "Event", field: "event", width: 100 },
                        { title: "Field", field: "fieldName", width: 190 },
                        { title: "Old Value", field: "oldValue" },
                        { title: "New Value", field: "newValue" }
                    ]
                });
            }
            historyModal.show();
            return;
        }

        const deleteButton = event.target.closest(".delete-btn");
        if (deleteButton) {
            const ok = window.confirm(`Delete ${deleteButton.dataset.reference}? This demo intentionally removes dependent audit/follow-up records first.`);
            if (!ok) return;

            const token = document.querySelector('#deleteTokenForm input[name="__RequestVerificationToken"]').value;
            const formData = new FormData();
            formData.append("__RequestVerificationToken", token);
            const response = await fetch(`${config.deleteBaseUrl}/${deleteButton.dataset.id}?demoUserId=${config.demoUserId}`, {
                method: "POST",
                body: formData
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                window.alert(result.message || "Delete failed.");
                return;
            }
            table.setData();
        }
    });
})();
