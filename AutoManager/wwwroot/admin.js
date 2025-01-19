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
    
    
    
    
    
    
    
    
    
    

});