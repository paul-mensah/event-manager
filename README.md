# Events Manager API (Microservices)

This is an API to manage events with participants and developed using .NET 6, Redis, and Elasticsearch and MySQL. 
This project contains 2 services; `EventsManager.Events.Api` and `EventsManager.Events.Api`

## Prerequirements
* Jetbrains Rider or Visual Studio 2019 (or higher)
* .NET Core SDK (version 6.0 or higher)
* Elasticsearch
* Redis
* MySQL

## Setup
to set it up.
* `.NET 6`: Download .NET 6 [here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and proceed to your OS link for installation guide
  * Windows Installation Guide: https://learn.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60
  * Linux Installation Guide: https://learn.microsoft.com/en-us/dotnet/core/install/linux
  * macOS Installation Guide: https://learn.microsoft.com/en-us/dotnet/core/install/macos
* `IDEs`: Click [here](https://www.jetbrains.com/rider/download/) to download Jetbrains Rider and 
[here](https://visualstudio.microsoft.com/downloads/) for Visual Studio 2022
* Run `docker compose up` in the docs folder to setup redis, mysql and elasticsearch in docker


## How To Run
* Open solution in Jetbrains Rider or Visual Studio
* Build the solution.
* Run the <b>EventsManager.Events.Api</b> project to manage events
* Run the <b>EventsManager.Invitations.Api</b> project to manage event invitations. <b>NB</b>: EventsManager.Events.Api should be running and update
the events api port in the appsettings of the invitations api