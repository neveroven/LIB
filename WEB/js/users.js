function setEditUser(id, login, isAdmin) {
            document.getElementById('editUserId').value = id;
            document.getElementById('editUserLogin').value = login;
            document.getElementById('editIsAdmin').checked = isAdmin;
        }

        function setChangePasswordUser(id, login) {
            document.getElementById('changePasswordUserId').value = id;
            document.getElementById('changePasswordUserLogin').value = login;
        }

        // Очистка формы добавления при закрытии модального окна
        document.getElementById('addUserModal').addEventListener('hidden.bs.modal', function () {
            this.querySelector('form').reset();
        });