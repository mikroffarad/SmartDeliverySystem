# Smart Delivery System - React + TypeScript

Interactive delivery tracking system with real-time visualization on a world map.

**Project Flow:**
1. Full-scale world map with initial focus on Ukraine
2. Users can create two types of markers: **vendors** (suppliers) and **stores**
3. **Vendor popup functionality:** add/edit/delete products, create delivery requests, delete vendor (if no products/deliveries exist)
4. **Store popup functionality:** view inventory (products added only via deliveries), delete store (if no products/deliveries exist)
5. **Delivery process:** select products and quantities → choose store (manual or "Auto-select best store") → payment → automatic driver/GPS assignment
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
- **Testing:** xUnit (unit and integration tests)

**Architecture:**
```
Frontend (React) ↔ Web API ↔ SQL Server
                      ↓
               Azure Functions (Timer) ↔ Table Storage
                      ↓
               SignalR Hub (Real-time updates)
                      ↓
               OSRM Backend (Routing)
```

How to run:
1. Clone repo
2. **Setup OSRM Docker container:**
   ```bash
   docker run -t -i -p 5000:5000 -v "${PWD}:/data" osrm/osrm-backend osrm-extract -p /opt/car.lua /data/ukraine-latest.osm.pbf
   docker run -t -i -p 5000:5000 -v "${PWD}:/data" osrm/osrm-backend osrm-partition /data/ukraine-latest.osrm
   docker run -t -i -p 5000:5000 -v "${PWD}:/data" osrm/osrm-backend osrm-customize /data/ukraine-latest.osrm
   docker run -t -i -p 5000:5000 -v "${PWD}:/data" osrm/osrm-backend osrm-routed --algorithm mld /data/ukraine-latest.osrm
   ```
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
6. **Install frontend dependencies:**
   ```bash
   cd frontend
   npm install
   ```
7. **Run all services:**
   - Backend: `dotnet run` (SmartDeliverySystem)
   - Azure Functions: `func start` (SmartDeliverySystem.Azure.Functions)
   - Frontend: `npm run dev` (frontend directory)

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
  "notes": "На дорозі до магазину"
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

- [x] **Interactive world map** with Leaflet.js and Ukraine focus
- [x] **Vendor and Store management** with map markers and popups
- [x] **Product CRUD operations** through vendor popups
- [x] **Smart store selection algorithm** (distance + inventory optimization)
- [x] **Payment processing** and automatic driver assignment
- [x] **Real-time truck tracking** with route visualization
- [x] **Azure Timer Functions** for truck movement simulation
- [x] **SignalR** for real-time updates
- [x] **GPS history** stored in Azure Table Storage
- [x] **OSRM integration** for route calculation
- [x] **Active Deliveries** monitoring section
- [x] **Complete delivery history** with GPS tracking details

## Auto-Select Best Store Algorithm
The system automatically selects the optimal store based on two criteria:
1. **Distance** from vendor to store (shorter = better)
2. **Current inventory** in the store (less = better)

This ensures optimal product distribution and minimizes delivery time.

## Azure Architecture

```
Web API → Azure Functions → SQL Database
                    ↓
               Table Storage (GPS History)
                    ↓
               SignalR Hub (Real-time updates)
                    ↓
               OSRM Backend (Routing)
```

## ⚠️ Security Notice

**Never commit real Azure connection strings to Git!**
- Use template files (`*.template.json`)
- Add real config files to `.gitignore`
- Use Azure Key Vault for production

## Project Structure

```
SmartDeliverySystem/              # Main Web API project
SmartDeliverySystem.Azure.Functions/  # Azure Functions (Timer triggers)
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
docker/                          # OSRM backend container setup
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
