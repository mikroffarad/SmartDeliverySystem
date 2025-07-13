import React, { useState, useEffect, useRef } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { MapComponent } from './components/MapComponentDebug';
import { AddLocationModal } from './components/AddLocationModal';
import { VendorProductsModal } from './components/VendorProductsModal';
import { StoreInventoryModal } from './components/StoreInventoryModal';
import { CreateDeliveryModal } from './components/CreateDeliveryModal';
import { PaymentModal } from './components/PaymentModal';
import { DriverAssignmentModal } from './components/DriverAssignmentModal';
import { DeliveryData, ConnectionStatus, LocationData } from './types/delivery';
import { deliveryApi } from './services/deliveryApi';
import './index.css';

const App: React.FC = () => {
    const [deliveryData, setDeliveryData] = useState<Record<string, DeliveryData>>({});
    const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');
    const [showAddModal, setShowAddModal] = useState(false);
    const [addingType, setAddingType] = useState<'vendor' | 'store' | null>(null);
    const [selectedLocation, setSelectedLocation] = useState<LocationData | null>(null); const [isAddingMode, setIsAddingMode] = useState(false);
    const [showProductsModal, setShowProductsModal] = useState(false);
    const [showInventoryModal, setShowInventoryModal] = useState(false);
    const [currentVendorId, setCurrentVendorId] = useState<number | null>(null);
    const [currentStoreId, setCurrentStoreId] = useState<number | null>(null);
    const [currentStoreName, setCurrentStoreName] = useState('');

    // Delivery workflow modals
    const [showCreateDeliveryModal, setShowCreateDeliveryModal] = useState(false);
    const [showPaymentModal, setShowPaymentModal] = useState(false);
    const [showDriverModal, setShowDriverModal] = useState(false);
    const [currentDeliveryId, setCurrentDeliveryId] = useState<number | null>(null);
    const [currentTotalAmount, setCurrentTotalAmount] = useState(0); const [currentVendorName, setCurrentVendorName] = useState('');
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    const connectionRef = useRef<HubConnection | null>(null);
    const updateTimeoutRef = useRef<NodeJS.Timeout | null>(null);

    useEffect(() => {
        initializeSignalR();
        loadActiveDeliveries();

        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
            }
        };
    }, []);

    const initializeSignalR = async () => {
        try {
            const connection = new HubConnectionBuilder()
                .withUrl("https://localhost:7183/deliveryHub")
                .configureLogging(LogLevel.Information)
                .build();

            connection.on("LocationUpdated", (data) => {
                updateDeliveryLocation(data);
            });

            connection.on("DeliveryStatusUpdated", (data) => {
                updateDeliveryStatus(data);
            });

            await connection.start();
            await connection.invoke("JoinAllDeliveries");

            connectionRef.current = connection;
            setConnectionStatus('connected');
        } catch (error) {
            console.error("SignalR Error:", error);
            setConnectionStatus('error');
        }
    }; const loadActiveDeliveries = async () => {
        try {
            console.log('📦 Loading active deliveries...');
            const deliveries = await deliveryApi.getActiveDeliveries();
            console.log('📦 Received deliveries from API:', deliveries);

            const deliveryMap: Record<string, DeliveryData> = {};

            deliveries.forEach(delivery => {
                console.log(`📦 Processing delivery ${delivery.deliveryId}, status: ${delivery.status}`);
                if (delivery.status !== 'Delivered' && delivery.status !== 'Cancelled') {
                    deliveryMap[delivery.deliveryId.toString()] = delivery;
                    console.log(`📦 Added delivery ${delivery.deliveryId} to map`);
                } else {
                    console.log(`📦 Skipped delivery ${delivery.deliveryId} with status ${delivery.status}`);
                }
            });

            console.log('📦 Final delivery map:', deliveryMap);
            setDeliveryData(deliveryMap);
        } catch (error) {
            console.error('Error loading deliveries:', error);
        }
    }; const updateDeliveryLocation = (data: any) => {
        console.log('📍 Location update received:', data);

        // Throttle updates to prevent too many re-renders
        if (updateTimeoutRef.current) {
            clearTimeout(updateTimeoutRef.current);
        }

        updateTimeoutRef.current = setTimeout(() => {
            setDeliveryData(prev => {
                const deliveryId = data.deliveryId.toString();
                console.log('📍 Updating delivery:', deliveryId, 'in data:', prev);
                if (prev[deliveryId]) {
                    const updated = {
                        ...prev,
                        [deliveryId]: {
                            ...prev[deliveryId],
                            currentLatitude: data.latitude,
                            currentLongitude: data.longitude,
                            lastLocationUpdate: new Date().toISOString()
                        }
                    };
                    console.log('📍 Updated delivery data:', updated);
                    return updated;
                } else {
                    console.log('📍 Delivery not found in current data - trying to fetch from API...');
                    // Спробуємо додати доставку, якщо її немає
                    loadDeliveryById(data.deliveryId);

                    // Також перезавантажимо всі активні доставки
                    setTimeout(() => {
                        console.log('📦 Reloading all active deliveries due to missing delivery...');
                        loadActiveDeliveries();
                    }, 100);

                    return prev;
                }
            });
        }, 100); // 100ms throttle
    };

    const loadDeliveryById = async (deliveryId: number) => {
        try {
            console.log(`📦 Loading delivery ${deliveryId} from API...`);
            const delivery = await deliveryApi.getDeliveryById(deliveryId);
            console.log(`📦 Received delivery ${deliveryId}:`, delivery);

            if (delivery && delivery.status !== 'Delivered' && delivery.status !== 'Cancelled') {
                setDeliveryData(prev => ({
                    ...prev,
                    [deliveryId.toString()]: delivery
                }));
                console.log(`📦 Added missing delivery ${deliveryId} to state`);
            }
        } catch (error) {
            console.error(`Error loading delivery ${deliveryId}:`, error);
        }
    };

    const updateDeliveryStatus = (data: any) => {
        const deliveryId = data.deliveryId.toString();

        if (data.status === 'Delivered' || data.status === 'Cancelled') {
            setDeliveryData(prev => {
                const newData = { ...prev };
                delete newData[deliveryId];
                return newData;
            });
        } else {
            setDeliveryData(prev => {
                if (prev[deliveryId]) {
                    return {
                        ...prev,
                        [deliveryId]: {
                            ...prev[deliveryId],
                            status: data.status
                        }
                    };
                }
                return prev;
            });
        }
    }; const handleAddVendor = () => {
        console.log('🏭 Add Vendor button clicked');
        setAddingType('vendor');
        setIsAddingMode(true);
        console.log('🏭 Set addingType to vendor, isAddingMode to true');
    };

    const handleAddStore = () => {
        console.log('🏪 Add Store button clicked');
        setAddingType('store');
        setIsAddingMode(true);
        console.log('🏪 Set addingType to store, isAddingMode to true');
    }; const handleLocationSelect = (lat: number, lng: number) => {
        console.log('🎯 handleLocationSelect called with:', lat, lng);
        console.log('🎯 isAddingMode:', isAddingMode, 'addingType:', addingType);

        if (isAddingMode && addingType) {
            console.log('🎯 Selected location for adding:', lat, lng);
            setSelectedLocation({ name: '', latitude: lat, longitude: lng });
            setShowAddModal(true);
            setIsAddingMode(false);
        } else {
            console.log('🎯 Not in adding mode or no adding type');
        }
    }; const handleSaveLocation = async (locationData: LocationData) => {
        try {
            if (addingType === 'vendor') {
                await deliveryApi.createVendor(locationData);
                alert('Vendor created successfully!');
            } else if (addingType === 'store') {
                await deliveryApi.createStore(locationData);
                alert('Store created successfully!');
            } setShowAddModal(false);
            setAddingType(null);
            setSelectedLocation(null);
            // Trigger map refresh
            setRefreshTrigger(prev => prev + 1);
        } catch (error) {
            console.error('Error saving location:', error);
            alert('Error saving location. Please try again.');
        }
    }; const handleCancelAdd = () => {
        setShowAddModal(false);
        setAddingType(null);
        setSelectedLocation(null);
        setIsAddingMode(false);
    }; const handleShowVendorProducts = (vendorId: number) => {
        console.log('Show vendor products:', vendorId);
        setCurrentVendorId(vendorId);
        setShowProductsModal(true);
    }; const handleShowStoreInventory = (storeId: number, storeName?: string) => {
        console.log('Show store inventory:', storeId, storeName);
        setCurrentStoreId(storeId);
        setCurrentStoreName(storeName || `Store #${storeId}`);
        setShowInventoryModal(true);
    }; const handleCreateDelivery = (vendorId: number) => {
        // Знаходимо назву вендора
        const vendorElement = document.querySelector(`[data-vendor-id="${vendorId}"]`);
        const vendorName = vendorElement?.getAttribute('data-vendor-name') || `Vendor #${vendorId}`;

        setCurrentVendorId(vendorId);
        setCurrentVendorName(vendorName);
        setShowCreateDeliveryModal(true);
    };

    const handleDeliveryCreated = (deliveryId: number, totalAmount: number) => {
        setCurrentDeliveryId(deliveryId);
        setCurrentTotalAmount(totalAmount);
        setShowCreateDeliveryModal(false);
        setShowPaymentModal(true);
    };

    const handlePaymentProcessed = (deliveryId: number) => {
        setShowPaymentModal(false);
        setShowDriverModal(true);
    }; const handleDriverAssigned = () => {
        setShowDriverModal(false);
        setCurrentDeliveryId(null);
        setCurrentTotalAmount(0);
        setCurrentVendorName('');
        // No need to reload - SignalR will update automatically
    };

    const handleCancelDelivery = async (deliveryId: number) => {
        try {
            await deliveryApi.cancelDelivery(deliveryId);
            alert('❌ Delivery has been cancelled');
            setShowPaymentModal(false);
            setShowDriverModal(false); setCurrentDeliveryId(null);
            setCurrentTotalAmount(0);
            setCurrentVendorName('');
            // No need to reload - SignalR will automatically update delivery status
        } catch (error) {
            console.error('Error cancelling delivery:', error);
            alert('Error cancelling delivery. Please try again.');
        }
    }; const handleMarkAsDelivered = async (deliveryId: number) => {
        if (!confirm(`Mark delivery #${deliveryId} as delivered? This will add products to store inventory.`)) {
            return;
        }

        try {
            await deliveryApi.updateDeliveryStatus(deliveryId, 4); // 4 = Delivered
            alert(`✅ Delivery #${deliveryId} has been marked as delivered!\n\n📦 Products have been added to store inventory.`);
            // No need to reload - SignalR will automatically remove the delivery from active list
        } catch (error) {
            console.error('Error marking delivery as delivered:', error);
            alert('Error marking delivery as delivered. Please try again.');
        }
    };

    return (
        <div className="app">
            {/* Map */}            <div id="map">                <MapComponent
                onLocationSelect={handleLocationSelect} deliveries={deliveryData}
                onShowVendorProducts={handleShowVendorProducts}
                onShowStoreInventory={handleShowStoreInventory}
                onCreateDelivery={handleCreateDelivery} onMarkAsDelivered={handleMarkAsDelivered}
                isAddingMode={isAddingMode}
                addingType={addingType}
                refreshTrigger={refreshTrigger}
            />
            </div>{/* Control buttons */}
            <div className="control-buttons">
                <button
                    onClick={handleAddVendor}
                    style={{
                        backgroundColor: isAddingMode && addingType === 'vendor' ? '#ffc107' : '#007bff',
                        color: isAddingMode && addingType === 'vendor' ? '#000' : '#fff'
                    }}
                >
                    🏭 {isAddingMode && addingType === 'vendor' ? 'Click on map to add vendor' : 'Add Vendor'}
                </button>
                <button
                    onClick={handleAddStore}
                    style={{
                        backgroundColor: isAddingMode && addingType === 'store' ? '#ffc107' : '#007bff',
                        color: isAddingMode && addingType === 'store' ? '#000' : '#fff'
                    }}
                >
                    🏪 {isAddingMode && addingType === 'store' ? 'Click on map to add store' : 'Add Store'}
                </button>
                {isAddingMode && (
                    <button
                        onClick={handleCancelAdd}
                        style={{ backgroundColor: '#dc3545', color: '#fff' }}
                    >
                        ✖️ Cancel
                    </button>
                )}
            </div>            {/* Delivery info panel */}
            <div className="delivery-info">
                <h3>📦 Active Deliveries</h3>
                <div className={`connection-status ${connectionStatus}`}>
                    SignalR: {connectionStatus === 'connected' ? '🟢 Connected' :
                        connectionStatus === 'disconnected' ? '🔴 Disconnected' :
                            '⚠️ Error'}
                </div>
                <div style={{ fontSize: '12px', color: '#666', marginBottom: '10px' }}>
                    Debug: {Object.keys(deliveryData).length} deliveries in state
                    {Object.keys(deliveryData).length > 0 && (
                        <div>IDs: {Object.keys(deliveryData).join(', ')}</div>
                    )}
                    <button
                        onClick={loadActiveDeliveries}
                        style={{ fontSize: '10px', padding: '2px 6px', marginTop: '5px' }}>
                        🔄 Reload Deliveries
                    </button>
                </div>

                {Object.keys(deliveryData).length === 0 ? (
                    <p>No active deliveries</p>
                ) : (
                    <div>
                        {Object.entries(deliveryData).map(([deliveryId, delivery]) => (
                            <div key={deliveryId} className="delivery-item">
                                <h4>🚛 Delivery #{deliveryId}</h4>
                                <p><strong>Status:</strong> {delivery.status}</p>
                                <p><strong>Driver:</strong> {delivery.driverId || 'Not assigned'}</p>
                                {delivery.currentLatitude && delivery.currentLongitude && (
                                    <p><strong>Location:</strong> {delivery.currentLatitude.toFixed(4)}, {delivery.currentLongitude.toFixed(4)}</p>
                                )}
                                {delivery.lastLocationUpdate && (
                                    <p><strong>Last Update:</strong> {new Date(delivery.lastLocationUpdate).toLocaleTimeString()}</p>
                                )}
                                {delivery.totalAmount && (
                                    <p><strong>Total:</strong> ${delivery.totalAmount.toFixed(2)}</p>
                                )}                                {delivery.status === 'InTransit' && (
                                    <button onClick={() => handleMarkAsDelivered(parseInt(deliveryId))}>
                                        ✅ Mark as Delivered
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                )}            </div>            <AddLocationModal
                isOpen={showAddModal}
                addingType={addingType}
                selectedLocation={selectedLocation}
                onSave={handleSaveLocation}
                onCancel={handleCancelAdd}
            />            <VendorProductsModal
                isOpen={showProductsModal}
                vendorId={currentVendorId}
                onClose={() => {
                    setShowProductsModal(false);
                    setCurrentVendorId(null);
                }}
            />

            <StoreInventoryModal
                isOpen={showInventoryModal}
                storeId={currentStoreId}
                storeName={currentStoreName}
                onClose={() => {
                    setShowInventoryModal(false);
                    setCurrentStoreId(null);
                    setCurrentStoreName('');
                }}
            />

            <CreateDeliveryModal
                isOpen={showCreateDeliveryModal}
                vendorId={currentVendorId}
                vendorName={currentVendorName}
                onClose={() => {
                    setShowCreateDeliveryModal(false);
                    setCurrentVendorId(null);
                    setCurrentVendorName('');
                }}
                onDeliveryCreated={handleDeliveryCreated}
            />

            <PaymentModal
                isOpen={showPaymentModal}
                deliveryId={currentDeliveryId}
                totalAmount={currentTotalAmount}
                onClose={() => setShowPaymentModal(false)}
                onPaymentProcessed={handlePaymentProcessed}
                onCancel={handleCancelDelivery}
            />

            <DriverAssignmentModal
                isOpen={showDriverModal}
                deliveryId={currentDeliveryId}
                onClose={() => setShowDriverModal(false)}
                onDriverAssigned={handleDriverAssigned}
                onCancel={handleCancelDelivery}
            />
        </div>
    );
};

export default App;
