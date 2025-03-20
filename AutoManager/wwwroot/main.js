document.addEventListener('DOMContentLoaded', () => {

    window.openForm = function openForm() {
        document.getElementById("myForm").style.display = "block";
    }

    window.closeForm = function closeForm() {
        document.getElementById("myForm").style.display = "none";
    }
    document.getElementById('title').placeholder = "Title"
    document.getElementById('description').placeholder = "Description"
    document.getElementById('id').placeholder = "ID of Requirement"

    document.getElementById('id2').placeholder = "ID of Requirement"
    
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
            .then(response => {
                // Check if the response is not OK (error)
                if (!response.ok) {
                    // Extract the full error message from the response body
                    return response.text().then(text => {
                        throw new Error(text);
                    });
                }
                return response.json();
            })
            .then(data => {
                fetchAPIData();
                document.getElementById("title").value = '';
                document.getElementById("description").value = '';
            })
            .catch(error => {
                alert(error.message);
                // display error message if new requirement is too similar. Then, clear fields.
                document.getElementById("title").value = '';
                document.getElementById("description").value = '';
            });
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
            
            .then(response => {
                if (response.ok) {
                    console.log(`${id} has been deleted.`)
                    fetchAPIData()
                    document.getElementById("id").value = '';
                }

            })
    }
    document.getElementById('deleteRequirement').addEventListener('click', deleteRow);

    
    
    function updateRow() {
        const id = document.getElementById('id2').value.trim();
        if (!id) {
            alert("Please enter ID to update")
            return;
        }
        
        // First we get the item by ID, then use its title and description to fill the updatedRequirement

        fetch(`${SERVER_URI}/${id}`,
            {
                method: 'GET'
            }
        )
            .then(res => res.json())
        .then(data => {
            console.log(data)
            const newTitle = prompt("Enter updated title:", data.title);
            if (newTitle === null) {
                // User cancelled the prompt
                return;
            }
            const newDescription = prompt("Enter updated description:", data.description);
            if (newDescription === null) {
                // User cancelled the prompt
                return;
            }
            
            const updatedRequirement = {
                id: data.id,
                title: newTitle.trim(),
                description: newDescription.trim()
            };
            if (!updatedRequirement.title || !updatedRequirement.description) {
                alert("Title and Description cannot be empty.");
                return;
            }
            // Update existing item
            fetch(SERVER_URI, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(updatedRequirement),
            })
                .then(response => {
                    // Check if the response is not OK (error)
                    if (!response.ok) {
                        // Extract the full error message from the response body
                        return response.text().then(text => {
                            throw new Error(text);
                        });
                    }
                    return response.json();
                })
                .then(data => {
                    fetchAPIData();
                })
                .catch(error => {
                    alert(error.message);
                    document.getElementById("id2").value = '';
            })
        })
    }
    document.getElementById('updateRequirement').addEventListener('click', updateRow);

});