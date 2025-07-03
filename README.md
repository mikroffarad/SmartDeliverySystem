# Smart Delivery System

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

**Test Data:**
The project includes a TestDataController that creates simplified test data:
- 1 vendor (Київський Продуктовий Центр) located in central Kyiv
- 5 products from this vendor (bread, milk, apples, eggs, buckwheat)
- 10 stores in different districts of Kyiv
- Each store has all products in different quantities (50-200 units)

How to run:
1. Clone repo
2. Open with Visual Studio or Visual Studio Code
3. **Copy configuration files:**
   ```bash
   cp SmartDeliverySystem/appsettings.template.json SmartDeliverySystem/appsettings.json
   cp SmartDeliverySystem.Azure.Functions/local.settings.template.json SmartDeliverySystem.Azure.Functions/local.settings.json
   ```
4. **Edit connection strings** in `appsettings.json` and `local.settings.json` with your actual values
5. Install Entity Framework tools and create database:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
6. Run project

## API Usage Examples

### 1. Seed Test Data
```bash
POST /api/testdata/seed
```

### 2. Request Delivery
```json
POST /api/delivery/request
{
  "vendorId": 1,
  "products": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}
```

### 3. Pay for Delivery
```json
POST /api/delivery/{id}/pay
{
  "amount": 83.00,
  "paymentMethod": "Card"
}
```

### 4. Assign Driver
```json
POST /api/delivery/{id}/assign-driver
{
  "driverId": "DRIVER001",
  "gpsTrackerId": "GPS001",
  "deliveryType": 0
}
```
*Note: Coordinates are automatically taken from the delivery record (vendor → store route)*

### 5. Update GPS Location
```json
POST /api/delivery/{id}/update-location
{
  "latitude": 50.4501,
  "longitude": 30.5234,
  "speed": 60.5,
  "notes": "На дорозі до магазину"
}
```

### 6. Get Delivery Tracking
```bash
GET /api/delivery/{id}/tracking
```

### 7. Get All Active Tracking
```bash
GET /api/delivery/tracking/active
```

## Delivery Flow

1. **Vendor** sends delivery request → System finds best store
2. **System** responds with delivery details and store location
3. **Vendor** pays for delivery → System processes payment
4. **Vendor** assigns driver and GPS tracker → System starts tracking
5. **Driver** updates location → System tracks in real-time
6. **System** provides tracking info to all stakeholders

## Next Steps

- [x] **Azure Functions integration** ✅
- [x] **Service Bus for async processing** ✅
- [x] **Table Storage for GPS history** ✅
- [x] **SignalR for real-time updates** ✅
- [x] Frontend map visualization

## Azure Architecture

```
Web API → Service Bus → Azure Functions → SQL Database
                    ↓
               Table Storage (GPS History)
                    ↓
               SignalR Hub (Real-time updates)
```

## ⚠️ Security Notice

**Never commit real Azure connection strings to Git!**
- Use template files (`*.template.json`)
- Add real config files to `.gitignore`
- Use Azure Key Vault for production
