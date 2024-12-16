document.addEventListener('DOMContentLoaded', () => {

    
    
    document.getElementById('title').placeholder = "Title"
    document.getElementById('description').placeholder = "Description"
    document.getElementById('id').placeholder = "ID of Requirement"
    
    
    
    const SERVER_URI = 'http://localhost:5167/api/Requirements'
    
    function fetchAPIData() {
        fetch(SERVER_URI)
            //.then(res => console.log(res))
            .then(res => res.json())
            .then(data => {
                console.log(data)
                const table = document.getElementById('tableRows')
                table.innerHTML = '';
                data.forEach(requirement => {
                    const row = document.createElement('tr');
                    row.setAttribute('data-id', requirement.id);
                    row.innerHTML = `
                    <td>${requirement.id}</td>
                    <td>${requirement.title}</td>
                    <td>${requirement.description}</td>
                `;
                    table.appendChild(row);
                })
            })
            .catch(error => console.log(error))
    }
    fetchAPIData();
    
    function createRequirementObjectFromInput() {
        return {
            id: 0, 
            "title": document.getElementById("title").value,
            "description": document.getElementById("description").value,
        };
    }
    
    function insertRow() {
        const requirement = createRequirementObjectFromInput()
        
        if (!requirement.title || !requirement.description) {
            alert("Please enter title and description");
            return;
        }
        
        fetch(SERVER_URI, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requirement),
        })
            .then(res => res.json())
            .then(data => {
                fetchAPIData();
                document.getElementById("title").value = '';
                document.getElementById("description").value = '';
            })
        
    }
    document.getElementById('addRequirement').addEventListener('click', insertRow);
    
    
    function deleteRow() {
        const id = document.getElementById('id').value.trim();
        if (!id) {
            alert("Please enter ID for deletion")
            return;
        }
        fetch(`${SERVER_URI}/${id}`,
            {
                method: 'DELETE',})
        // remove visual display of row

            .then(response => {
                if (response.ok) {
                    console.log(`${id} has been deleted.`)
                    fetchAPIData()
                    document.getElementById("id").value = '';
                }

            })
    }
    document.getElementById('deleteRequirement').addEventListener('click', deleteRow);



});