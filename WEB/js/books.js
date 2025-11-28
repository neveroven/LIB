// Обработка модального окна редактирования
        const editBookModal = document.getElementById('editBookModal');
        if (editBookModal) {
            editBookModal.addEventListener('show.bs.modal', function (event) {
                const button = event.relatedTarget;
                document.getElementById('edit_id').value = button.getAttribute('data-id');
                document.getElementById('edit_title').value = button.getAttribute('data-title');
                document.getElementById('edit_author').value = button.getAttribute('data-author');
                document.getElementById('edit_published_year').value = button.getAttribute('data-published_year');
                document.getElementById('edit_description').value = button.getAttribute('data-description');
                document.getElementById('edit_language').value = button.getAttribute('data-language');
                document.getElementById('edit_series').value = button.getAttribute('data-series');
                document.getElementById('edit_series_index').value = button.getAttribute('data-series_index');
            });
        }