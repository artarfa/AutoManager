document.addEventListener('DOMContentLoaded', () => 
{
    const SERVER_URI = 'http://localhost:5167/api/Account/login'
    const loginbutton = document.getElementById('loginButton')

    loginbutton.addEventListener('click', async (event) => {
        event.preventDefault()

        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        try {
            const response = await fetch(SERVER_URI, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({email, password})
            });
            console.log(response)

            if (!response.ok) {
                alert("Invalid credentials")
                return;
            }

            alert("login successful");
            window.location.href = 'index.html';
        } catch (error) {
            console.log("Error:", error);
        }
    });
    

});