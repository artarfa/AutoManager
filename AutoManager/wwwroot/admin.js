document.addEventListener('DOMContentLoaded', () => {




    const SERVER_URI = 'http://localhost:5167/api/Account'
    
    
    function fetchData() {
        fetch(SERVER_URI)
        
            .then(res => res.json())
        .then(data => {
            console.log(data)
            const table = document.getElementById('userTableRows')
            table.innerHTML = '';
            data.forEach(user => {
                const row = document.createElement('tr');
                row.setAttribute('email', user.email);
                row.innerHTML = `
                <td>${user.email}</td>
                <td>${user.roles}</td>`;
                table.appendChild(row);
            })
        })
            .catch(err => console.log(err));
    }
    fetchData();
    
    function updateRole() {
        const email = document.getElementById('email').value.trim();
        const newRole = document.getElementById('newRole').value.trim();

        if (!email || !newRole) {
            alert('Please enter both email and updated role');
            return;
        }
        const roleData = {
            email: email,
            role: newRole
        }
        try {
            fetch(`${SERVER_URI}/assign-role`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(roleData)
            })
                .then(updatedData => {
                    fetchData();
                    document.getElementById("email").value = '';
                    document.getElementById("newRole").value = '';
            })
        }
        catch(err) {
            console.log(err);
        }
    }
    
    document.getElementById('updateRequirement').addEventListener('click', updateRole);
    
    function removeRole() {
        const email = document.getElementById('removalEmail').value.trim();
        const removalRole = document.getElementById('removalRole').value.trim();
        
        if (!email || !removalRole) {
            alert('Please enter both email and removal role');
        }
        
        const roleData = {
            email: email,
            role: removalRole
        }
        
        try {
            fetch(`${SERVER_URI}/remove-role`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(roleData)
            })
            .then(updatedData => {
                fetchData();
                document.getElementById("removalEmail").value = '';
                document.getElementById("removalRole").value = '';
            })
        }
        catch(err) {
            console.log(err);
        }
    }
    
    document.getElementById('deleteRequirement').addEventListener('click', removeRole);
    
    
    
    
    
    
    

});