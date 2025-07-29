<h2 align="center">
  <img src="SmartDeliverySystem.Frontend\src\images\favicon.ico" alt="Easy Effects icon" width="150" height="150"/>
  <br>
  Smart Delivery System
</h2>

<p align="center">
  <strong>Interactive delivery tracking system with real-time visualization on a world map.</strong>
</p>

**Project Flow:**
1. Full-scale world map with initial focus on Ukraine
2. Users can create two types of markers: **vendors** (suppliers) and **stores**
3. **Vendor popup functionality:**
    - add/edit/delete products,
    - create delivery requests,
    - delete vendor (if no products/deliveries exist).
4. **Store popup functionality:**
    - view inventory (products added only via deliveries),
    - delete store (if no products/deliveries exist).
5. **Delivery process:**
    - select products and quantities,
    - choose store (manual or "Auto-select best store"),
    - payment,
    - automatic driver/GPS assignment.
6. **Auto-select algorithm:** finds best store based on distance (shorter = better) and current inventory (less = better)
7. Truck icon appears near vendor with route visualization to destination store
8. Azure Timer Function moves truck along route every second
9. Upon arrival, popup shows "delivery completed" for few seconds, then truck/route disappear
10. Products are automatically added to store inventory
11. **Active Deliveries** section shows current deliveries in progress
12. **All Deliveries** section shows complete delivery history with GPS tracking details

**Technical Stack:**
- **Frontend:** React + TypeScript + Vite + Leaflet.js + SignalR
- **Backend:** ASP.NET Core Web API + Entity Framework
- **Database:** SQL Server (main data) + Azure Table Storage (GPS history)
- **Azure Functions:** Timer Trigger for truck movement simulation
- **Real-time:** SignalR Hub
- **Routing:** OSRM Backend (Docker container)
- **Testing:** xUnit, Moq (unit and integration tests)

**Architecture:**
```
Frontend (React) ↔ Web API ↔ SQL Server
                      ↓
               SignalR Hub (Real-time updates)
                      ↓
               Azure Functions (Timer) ↔ Table Storage (GPS History)
                      ↓
               OSRM Backend (Routing)
```

How to run:
1. Clone repo

2. **Create Azure Resources (portal.azure.com):**

   **2.1. Create Resource Group:**
   - Search "Resource groups" → Create
   - Name: `smart-delivery-rg`
   - Region: `Poland Central`
   - Click "Review + create" → Create

   **2.2. Create Storage Account:**
   - Search "Storage accounts" → Create
   - Resource group: `smart-delivery-rg`
   - Name: `smartdeliverystorage` (globally unique)
   - Region: `Poland Central`
   - Performance: Standard, Redundancy: LRS
   - Click "Review + create" → Create

   **2.3. Create Service Bus Namespace:**
   - Search "Service Bus" → Create
   - Resource group: `smart-delivery-rg`
   - Name: `smart-delivery-bus`
   - Region: `Poland Central`
   - Pricing tier: Basic
   - Click "Review + create" → Create

   **2.4. Create Function App:**
   - Search "Function App" → Create
   - Resource group: `smart-delivery-rg`
   - Name: `smart-delivery-functions`
   - Runtime stack: .NET, Version: 8 (LTS), Isolated
   - Region: `West Europe`
   - Storage: Use existing `smartdeliverystorage`
   - Click "Review + create" → Create

   **2.5. Create Service Bus Queues:**
   - Go to `smart-delivery-bus` → Queues
   - Create queue: `delivery-requests`
   - Create queue: `location-updates`

3. **Download OSM data and setup OSRM Docker container:**
   ```bash
   # Navigate to OSRM directory and download Ukraine OSM data
   cd SmartDeliverySystem.OSRM
   wget http://download.geofabrik.de/europe/ukraine-latest.osm.pbf

   # Build OSRM routing engine (4 steps)
   docker run -t -v "${PWD}:/data" osrm/osrm-backend osrm-extract -p /opt/car.lua /data/ukraine-latest.osm.pbf
   docker run -t -v "${PWD}:/data" osrm/osrm-backend osrm-partition /data/ukraine-latest.osrm
   docker run -t -v "${PWD}:/data" osrm/osrm-backend osrm-customize /data/ukraine-latest.osrm

   # Run OSRM server (keep this running)
   docker run -t -i -p 5000:5000 -v "${PWD}:/data" osrm/osrm-backend osrm-routed --algorithm mld /data/ukraine-latest.osrm
   ```

4. **Copy configuration files:**
   ```bash
   cd SmartDeliverySystem
   cp SmartDeliverySystem/appsettings.template.json SmartDeliverySystem/appsettings.json
   cp SmartDeliverySystem.Azure.Functions/local.settings.template.json SmartDeliverySystem.Azure.Functions/local.settings.json
   ```

5. **Edit connection strings** in `appsettings.json` and `local.settings.json`:
   - **ServiceBus**: `smart-delivery-bus` → Shared access policies → RootManageSharedAccessKey → Connection string
   - **AzureStorage**: `smartdeliverystorage` → Access keys → key1 → Connection string
   - **DefaultConnection**: Your SQL Server connection string

6. Install Entity Framework tools and create database:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

7. **Install frontend dependencies:**
   ```bash
   cd frontend
   npm install
   ```

8. **Run all services:**
   - Backend: `dotnet run --launch profile "https"` (SmartDeliverySystem)
   - Azure Functions: `func start` (SmartDeliverySystem.Azure.Functions, make sure you have [azure-functions-core-tools](https://github.com/Azure/azure-functions-core-tools) installer)
   - Frontend: `npm run dev` (frontend directory)

9. **Deploy Azure Functions:**
   ```bash
   cd SmartDeliverySystem.Azure.Functions
   func azure functionapp publish smart-delivery-functions
   ```

## API Usage Examples

### 1. Request Delivery
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

### 2. Pay for Delivery
```json
POST /api/delivery/{id}/pay
{
  "amount": 83.00,
  "paymentMethod": "Card"
}
```

### 3. Assign Driver
```json
POST /api/delivery/{id}/assign-driver
{
  "driverId": "DRIVER001",
  "gpsTrackerId": "GPS001",
  "deliveryType": 0
}
```
*Note: Coordinates are automatically taken from the delivery record (vendor → store route)*

### 4. Update GPS Location
```json
POST /api/delivery/{id}/update-location
{
  "latitude": 50.4501,
  "longitude": 30.5234,
  "speed": 60.5,
  "notes": "Movement"
}
```

### 5. Get Delivery Tracking
```bash
GET /api/delivery/{id}/tracking
```

### 6. Get All Active Tracking
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

## Features Implemented

- [x] **Core delivery functionality** with vendor-to-store routing ✅
- [x] **GPS tracking system** with real-time location updates ✅
- [x] **Azure Functions integration** with ServiceBus and Timer triggers ✅
- [x] **Service Bus async processing** for delivery and location updates ✅
- [x] **SignalR real-time tracking** with WebSocket updates ✅
- [x] **Interactive delivery map** with live GPS visualization ✅
- [x] **Table Storage** for GPS history and route data ✅
- [x] **Automatic drone movement** simulation with linear flight paths ✅

## Azure Architecture

```
Web API → Service Bus → Azure Functions → SQL Database
                    ↓
               Table Storage (GPS History)
                    ↓
               SignalR Hub (Real-time updates)
                    ↓
               Interactive Map (Live tracking)
```

## ⚠️ Security Notice

**Never commit real Azure connection strings to Git!**
- Use template files (`*.template.json`)
- Add real config files to `.gitignore`
- Use Azure Key Vault for production

## Azure Resources Overview

Your Azure setup should include these resources:
- **smart-delivery-rg**: Resource Group (Poland Central)
- **smartdeliverystorage**: Storage Account (Table Storage + Functions storage)
- **smart-delivery-bus**: Service Bus Namespace (async messaging)
- **smart-delivery-functions**: Function App (West Europe, .NET 8 Isolated)

All resources use the same Resource Group for easy management and cost tracking.

## Project Structure

```
SmartDeliverySystem/              # Main Web API project
SmartDeliverySystem.Azure.Functions/  # Azure Functions
SmartDeliverySystem.Tests/        # Unit and Integration tests
frontend/                         # React + TypeScript frontend
├── src/
│   ├── components/              # React components
│   │   ├── MapContainer.tsx     # Leaflet map component
│   │   ├── DeliveryInfo.tsx     # Delivery information display
│   │   ├── ControlButtons.tsx   # Control buttons
│   │   ├── AddLocationModal.tsx # Location addition modal
│   │   ├── ProductsModal.tsx    # Product management modal
│   │   ├── StoreProductsModal.tsx # Store inventory modal
│   │   ├── DeliveryModal.tsx    # Delivery request modal
│   │   ├── PaymentModal.tsx     # Payment processing modal
│   │   └── DriverModal.tsx      # Driver assignment modal
│   ├── services/
│   │   └── deliveryApi.ts       # API service for REST requests
│   ├── types/
│   │   └── delivery.ts          # TypeScript type definitions
│   ├── App.tsx                  # Main component
│   ├── App.css                  # Application styles
│   ├── main.tsx                 # Entry point
│   └── index.css                # Global styles
SmartDeliverySystem.OSRM/        # OSRM backend container setup
```

## Implementation Status

The project successfully implements:
1. ✅ Modular React structure with TypeScript
2. ✅ API service for REST requests
3. ✅ Real-time SignalR integration
4. ✅ Interactive Leaflet map
5. ✅ Form handling and modal windows
6. ✅ Comprehensive delivery tracking system

## Benefits of Current Architecture
- Modular and reusable code structure
- TypeScript type safety
- Fast development with Vite
- Easy maintenance and extension
- Clear separation of concerns
