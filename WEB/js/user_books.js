function filterTable() {
            const statusFilter = document.getElementById('statusFilter').value;
            const userFilter = document.getElementById('userFilter').value.toLowerCase();
            const rows = document.querySelectorAll('#userBooksTable tbody tr');
            
            let visibleCount = 0;
            
            rows.forEach(row => {
                const status = row.getAttribute('data-status');
                const user = row.getAttribute('data-user').toLowerCase();
                
                const statusMatch = !statusFilter || status === statusFilter;
                const userMatch = !userFilter || user.includes(userFilter);
                
                if (statusMatch && userMatch) {
                    row.style.display = '';
                    visibleCount++;
                } else {
                    row.style.display = 'none';
                }
            });
            
            // Обновляем заголовок с количеством
            const title = document.querySelector('.card-title');
            title.textContent = `Список книг пользователей (${visibleCount})`;
        }
        
        function resetFilters() {
            document.getElementById('statusFilter').value = '';
            document.getElementById('userFilter').value = '';
            filterTable();
        }
        
        // Применяем фильтры при изменении значений
        document.getElementById('statusFilter').addEventListener('change', filterTable);
        document.getElementById('userFilter').addEventListener('input', filterTable);