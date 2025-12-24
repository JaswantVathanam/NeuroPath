// Modal management functions for Therapist Dashboard

function showNotesModal() {
    const modal = new bootstrap.Modal(document.getElementById('notesModal'));
    modal.show();
}

function closeNotesModal() {
    const modal = bootstrap.Modal.getInstance(document.getElementById('notesModal'));
    if (modal) {
        modal.hide();
    }
}
