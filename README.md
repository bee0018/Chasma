# Chasma

## Dev Setup
- For version control download [SourceTree](https://www.sourcetreeapp.com/) for preferred OS
- Download [Node.js](https://nodejs.org/en/download/).
- Download [WebStorm](https://www.jetbrains.com/webstorm/promo/?source=google&medium=cpc&campaign=AMER_en_US-CST_WebStorm_Branded&term=webstorm&content=717267885243&gad_source=1&gad_campaignid=9641686287&gbraid=0AAAAADloJzjM8YwGuomM1PAAElS0TYUtX&gclid=Cj0KCQjw2IDFBhDCARIsABDKOJ6ZAX4ejAlVLQhNXvOmGcZ6rUg8tSvULOMdTD4DFZLUPnMA5E5bJkIaAmStEALw_wcB). Suggested that you use the non-commercial version for the time being.
- Download the [.NET8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) SDK. This will be used for running the backend and other dependencies such as the runtime environment.
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

## Extra Documentation
- For the video I used to help start this project: https://www.youtube.com/watch?v=dFIfZBEyZ90&t=1209s
- For setting up the endpoints and basis for talking to frontend to backend: https://fast-endpoints.com/docs/get-started
- For making the endpoints secure: https://fast-endpoints.com/docs/security#introduction.
	- At this point this has not been implemented but will need to be done.
