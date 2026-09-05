(() => {
    const form = document.querySelector("#dispatchForm");
    const submitButton = document.querySelector("#submitButton");
    const followUps = document.querySelector("#followUps");
    const addFollowUpButton = document.querySelector("#addFollowUp");

    const errorModal = new bootstrap.Modal(document.querySelector("#errorModal"));
    const successModal = new bootstrap.Modal(document.querySelector("#successModal"));

    const renumberFollowUps = () => {
        [...followUps.querySelectorAll(".followup-row")].forEach((row, index) => {
            row.querySelectorAll("input").forEach(input => {
                input.name = input.name.replace(/FollowUps\[\d+\]/, `FollowUps[${index}]`);
            });
        });
    };

    addFollowUpButton?.addEventListener("click", () => {
        const index = followUps.querySelectorAll(".followup-row").length;
        const row = document.createElement("div");
        row.className = "followup-row row g-2 align-items-end";
        row.innerHTML = `
            <input type="hidden" name="FollowUps[${index}].Id" value="0" />
            <div class="col-md-3"><label class="form-label">Date / time</label><input type="datetime-local" class="form-control" name="FollowUps[${index}].DateTime" /></div>
            <div class="col-md-3"><label class="form-label">Who</label><input class="form-control" name="FollowUps[${index}].Who" /></div>
            <div class="col-md-5"><label class="form-label">Outcome</label><input class="form-control" name="FollowUps[${index}].Outcome" /></div>
            <div class="col-md-1 d-grid"><button type="button" class="btn btn-outline-danger remove-followup">Remove</button></div>`;
        followUps.appendChild(row);
    });

    followUps?.addEventListener("click", (event) => {
        const button = event.target.closest(".remove-followup");
        if (!button) return;
        button.closest(".followup-row").remove();
        renumberFollowUps();
    });

    const validateForm = () => {
        const errors = [];
        for (const input of form.querySelectorAll("[required]")) {
            if (!input.value.trim()) errors.push(`${input.labels?.[0]?.textContent || input.name} is required.`);
        }
        return errors;
    };

    const showErrors = (errors) => {
        const list = document.querySelector("#errorMessages");
        list.innerHTML = "";
        (errors.length ? errors : ["Unable to save the dispatch."]).forEach(error => {
            const item = document.createElement("li");
            item.textContent = error;
            list.appendChild(item);
        });
        errorModal.show();
    };

    async function handleFormSubmission(event) {
        event.preventDefault();
        if (submitButton.disabled) return;

        submitButton.disabled = true;
        const originalText = submitButton.textContent;
        submitButton.textContent = "Saving…";

        const validationErrors = validateForm();
        if (validationErrors.length) {
            showErrors(validationErrors);
            submitButton.disabled = false;
            submitButton.textContent = originalText;
            return;
        }

        try {
            const response = await fetch(form.action, {
                method: form.method,
                body: new FormData(form)
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                showErrors(result.errors || [result.message || "Unable to save the dispatch."]);
                submitButton.disabled = false;
                submitButton.textContent = originalText;
                return;
            }

            successModal.show();
            const dashboardUrl = document.querySelector("#dashboardUrl").value;
            window.setTimeout(() => window.location.href = dashboardUrl, 650);
        } catch (error) {
            showErrors([error.message || "Unexpected error while saving."]);
            submitButton.disabled = false;
            submitButton.textContent = originalText;
        }
    }

    form.addEventListener("submit", handleFormSubmission);
})();
