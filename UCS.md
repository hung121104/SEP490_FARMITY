| Mục (Field) | Nội dung chi tiết (Description) |
| :--- | :--- |
| **UC ID and Name** | **UC-G-01: Register In-game** |
| **Created By** |  |
| **Date Created** |  |
| **Primary Actor** | New Player |
| **Secondary Actors** | None |
| **Trigger** | New Player selects the "Register" option from the game menu. |
| **Description** | This use case allows a new player to create a new in-game account by providing the required information, such as username and password. The system validates the data and creates a new account for the player. |
| **Preconditions** | **PRE-1.** The game is launched successfully.<br>**PRE-2.** The player does not have an existing in-game account.<br>**PRE-3.** The registration screen is accessible. |
| **Postconditions** | **POST-1.** A new player account is created and stored in the system.<br>**POST-2.** The player can log in using the newly created account. |
| **Normal Flow** | **1.0 Register In-game**<br>1. New Player selects Register In-game.<br>2. The system displays the registration form.<br>3. The new Player enters the required information (username, password). **E3**<br>4. The system validates the input data. **E4**<br>5. The system saves the new account to the database. **E5**<br>6. The system displays a successful registration message. |
| **Alternative Flows**| None |
| **Exceptions** | **E3.** If required fields are missing/invalid, the system returns a validation error.<br>**E4.** If the username is already taken then prompt the user to use another name and go back to step 3.<br>**E5.** If the database operation fails, the system returns "Failed to create account." |
| **Priority** | High |
| **Frequency of Use** | High |
| **Business Rules** | BR-01 |
| **Other Information** | None |
| **Assumptions** | None |