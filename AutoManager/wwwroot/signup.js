document.addEventListener('DOMContentLoaded', () =>
{
    const SERVER_URI = 'http://localhost:5167/api/Account/register'
    const signupButton = document.getElementById('signupButton')

    signupButton.addEventListener('click', async (event) => {
        event.preventDefault()

        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        try {
            const response = await fetch(SERVER_URI, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({email, password})
            });

            if (!response.ok) {
                const errorMessage = await response.text();
                alert(errorMessage)
                return;
            }
            const msg = await response.text();
            alert(msg);

            window.location.href = 'index.html';
        } catch (error) {
            console.log("Error:", error);
        }
    });


});