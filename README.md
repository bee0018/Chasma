# Chasma

## Dev Setup
- For version control download [SourceTree](https://www.sourcetreeapp.com/) for preferred OS
- Download [Node.js](https://nodejs.org/en/download/).
- Download [WebStorm](https://www.jetbrains.com/webstorm/promo/?source=google&medium=cpc&campaign=AMER_en_US-CST_WebStorm_Branded&term=webstorm&content=717267885243&gad_source=1&gad_campaignid=9641686287&gbraid=0AAAAADloJzjM8YwGuomM1PAAElS0TYUtX&gclid=Cj0KCQjw2IDFBhDCARIsABDKOJ6ZAX4ejAlVLQhNXvOmGcZ6rUg8tSvULOMdTD4DFZLUPnMA5E5bJkIaAmStEALw_wcB). Suggested that you use the non-commercial version for the time being.
- Download the [.NET8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) SDK. This will be used for running the backend and other dependencies such as the runtime environment.
- Download the [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads#trysql) database platform. The tool to interact with the database will be [Micorsoft SQL Server Management Studio](https://learn.microsoft.com/en-us/ssms/install/install).
- Download IDE for C# development. Preferred is [Visual Studio](https://visualstudio.microsoft.com/vs/professional/) or [Rider](https://www.jetbrains.com/rider/download/?section=windows).
	- If using Visual Studio, make sure to install the workloads for:
 		- ASP.NET and web development
   		- Azure development
     	- .NET Multi-platform App UI development
      	- .NET desktop development
      	- Desktop development with C++
      	- WinUI application development
- Navigate to the ChasmaWebApi directory in your terminal and enter the following command to install nswag dependencies:
    ```
    dotnet tool install --global NSwag.ConsoleCore
    ```
- Run the following command to ensure nswag has been installed successfully:
	```
	nswag --version
	```

## Running the Front End
- In your terminal, `cd chasma-thin-client` and enter `npm run start`.

## Developer Information
- We want to be able to have our typescript to be able to automatically know what the requests/responses look like for web app so it'll be easier to send requests and receive responses.
  - To do so, we will use NSwag Swagger Generation to build our objects (requests, responses, etc.) in our ChasmaWebAPI and output them to our `API/ChasmaWebApi.ts` file so we can use them in our web application.
  - To make updates to objects, all we need to do is update them in our backend web API and build the web API. Once the web api finishes building, all we need to do is refresh our chasma-web app to pickup changes.

## Database
- We will be using Entity Framework Core for database operations. Doing so will require SQL Server Express database installations.

### Adding/Updating Tables in the Database via Visual Studio
- We are able to add/update tables in the database without having to write database scripts. To do so, we must make models that Entity Framework Core will translate and migrate into the database. Note the following example:

1. Create a database schema(table) to be added to the database.
```c#
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the user_accounts table in the database.
    /// </summary>
    [Table("user_accounts")]
    public class UserAccount
    {
        /// <summary>
        /// Gets or sets the identifier of the account user.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets  the name of the account.
        /// </summary>
        [Column("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user name of the account.
        /// </summary>
        [Column("user_name")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password of the account.
        /// </summary>
        [Column("password")]
        public string Password { get; set; }
    }
}

```
2. Open the Package Manager Console:
<img width="945" height="556" alt="image" src="https://github.com/user-attachments/assets/a7c021d2-1f9a-43dc-a15b-96d81805a564" />

3. Add the migration using your custom name of choice. Refer to the example :  `Add-Migration ChasmaIntegration`
4. After the build succeeds, you should see the latest integration files in the `Migrations` folder. Refer to the example:
<img width="2372" height="685" alt="image" src="https://github.com/user-attachments/assets/f72c301a-cfd3-43bb-9b37-e309b6bb3c58" />

5. In the Package Manager Console, input the following to update/add the tables: `Update-Database`
6. In SSMS, you will see the reflected changes. Refer to the screenshot:
<img width="632" height="707" alt="image" src="https://github.com/user-attachments/assets/d201128b-bdfa-4bcb-ab15-3de7c0988fac" />

