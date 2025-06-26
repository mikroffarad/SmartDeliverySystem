# Smart Delivery Scheduling System for Local Stores

Automated system, which tracks delivery and process them.

**Project flow:**
1) Vendor pings our application, that delivery is ready.
Attaches a list of products, which are ready to go.
2) Application checks which store more relevant for this cargo.
3) Application responds with the most quick way to deliver the cargo.
Attaches a location of desired store.
4) Vendor bills Application with correspondent sum.
5) Application proceed the payment.
6) Vendor attaches DriverId, DeliveryType, Location(departed from), Location(where to), GpsTrackerId
7) Application, connects to delivery.
8) Application should track each Delivery and display it on map

**Technical stack:**
- *Azure Functions(ServiceBusTrigger func, TableStorageTrigger func, TimerTrigger func?)
- TableStorage
- SQL Server
- Microservices
- SignalR?*

How to run:
1. Clone repo
2. Open with Visual Studio
3. Edit connection string in `appsettings.json`
3. Tools > NuGet Package Manager > Package Manager Console
4. Type:
```
Add-Migration InitialCreate
Update-Database
```
Or:
```
dotnet tool install --global dotnet-ef
dotnet ef --version
dotnet ef migrations add InitialCreate
dotnet ef database update
```
5. Run project
