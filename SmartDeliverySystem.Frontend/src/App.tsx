import React, { useState, useEffect, useRef } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { MapComponent } from './components/MapComponent';
import { AddLocationModal } from './components/AddLocationModal';
import { VendorProductsModal } from './components/VendorProductsModal';
import { StoreInventoryModal } from './components/StoreInventoryModal';
import { CreateDeliveryModal } from './components/CreateDeliveryModal';
import { PaymentModal } from './components/PaymentModal';
import { DriverAssignmentModal } from './components/DriverAssignmentModal';
import { AllDeliveriesModal } from './components/AllDeliveriesModal';
import { DeliveryProductsModal } from './components/DeliveryProductsModal';
import { DeliveryData, ConnectionStatus, LocationData } from './types/delivery';
import { deliveryApi } from './services/deliveryApi';
import { getStatusText } from './utils/deliveryUtils';
import { ToastProvider, useToast } from './contexts/ToastContext';
import { ConfirmationProvider } from './contexts/ConfirmationContext';
import './index.css';

const AppContent: React.FC = () => {
    const { showSuccess, showError, showWarning, showInfo } = useToast();

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

    // All deliveries modal
    const [showAllDeliveriesModal, setShowAllDeliveriesModal] = useState(false);
    const [showDeliveryProductsModal, setShowDeliveryProductsModal] = useState(false);
    const [selectedDeliveryId, setSelectedDeliveryId] = useState<number | null>(null);

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
    };    // Функція для конвертації числових статусів в текстові
    const getStatusText = (status: number | string) => {
        if (typeof status === 'string') return status;

        switch (status) {
            case 0: return 'PendingPayment';
            case 1: return 'Paid';
            case 2: return 'Assigned';
            case 3: return 'InTransit';
            case 4: return 'Delivered';
            case 5: return 'Cancelled';
            default: return 'Unknown';
        }
    };

    const loadActiveDeliveries = async () => {
        try {
            console.log('📦 Loading active deliveries...');
            const deliveries = await deliveryApi.getActiveDeliveries();
            console.log('📦 Received deliveries from API:', deliveries);

            const deliveryMap: Record<string, DeliveryData> = {}; deliveries.forEach(delivery => {
                console.log(`📦 Processing delivery ${delivery.deliveryId}, status: ${delivery.status}`);
                // Показуємо доставки зі статусом Assigned (2) і InTransit (3)
                if (delivery.status === 2 || delivery.status === 3) {
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
                    return prev;
                }
            });
        }, 100); // 100ms throttle
    }; const loadDeliveryById = async (deliveryId: number) => {
        try {
            console.log(`📦 Loading delivery ${deliveryId} from API...`);
            const delivery = await deliveryApi.getDeliveryById(deliveryId);
            console.log(`📦 Received delivery ${deliveryId}:`, delivery);

            // Перевіряємо чи доставка має статус Assigned (2) або InTransit (3)
            if (delivery && (delivery.status === 2 || delivery.status === 3)) {
                setDeliveryData(prev => ({
                    ...prev,
                    [deliveryId.toString()]: delivery
                }));
                console.log(`📦 Added missing delivery ${deliveryId} to state`);
            } else {
                console.log(`📦 Delivery ${deliveryId} has status ${delivery?.status}, not adding to active deliveries`);
            }
        } catch (error) {
            console.error(`Error loading delivery ${deliveryId}:`, error);
        }
    }; const updateDeliveryStatus = (data: any) => {
        const deliveryId = data.deliveryId.toString();

        if (data.status === 4 || data.status === 5 || data.status === 'Delivered' || data.status === 'Cancelled') {
            // Видаляємо завершені доставки з активних
            setDeliveryData(prev => {
                const newData = { ...prev };
                delete newData[deliveryId];
                console.log(`📦 Removed completed delivery ${deliveryId} from active deliveries`);
                return newData;
            });
        } else if (data.status === 2 || data.status === 3) {
            // Додаємо або оновлюємо активні доставки
            setDeliveryData(prev => {
                if (prev[deliveryId]) {
                    return {
                        ...prev,
                        [deliveryId]: {
                            ...prev[deliveryId],
                            status: data.status
                        }
                    };
                } else {
                    // Якщо доставки немає, спробуємо її завантажити
                    loadDeliveryById(data.deliveryId);
                    return prev;
                }
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
                showSuccess('Vendor created successfully!');
            } else if (addingType === 'store') {
                await deliveryApi.createStore(locationData);
                showSuccess('Store created successfully!');
            } setShowAddModal(false);
            setAddingType(null);
            setSelectedLocation(null);
            // Trigger map refresh
            setRefreshTrigger(prev => prev + 1);
        } catch (error) {
            console.error('Error saving location:', error);
            showError('Error saving location. Please try again.');
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
            showSuccess('Delivery has been cancelled');
            setShowPaymentModal(false);
            setShowDriverModal(false);
            setCurrentDeliveryId(null);
            setCurrentTotalAmount(0);
            setCurrentVendorName('');
            // No need to reload - SignalR will automatically update delivery status
        } catch (error) {
            console.error('Error cancelling delivery:', error);
            showError('Error cancelling delivery. Please try again.');
        }
    }; const handleDeliveryArrived = (deliveryId: string) => {
        console.log(`🎯 Delivery ${deliveryId} arrived - removing from state`);
        setDeliveryData(prev => {
            const updated = { ...prev };
            delete updated[deliveryId];
            return updated;
        });
    };

    const testArrival = async (deliveryId: number) => {
        // Тестова функція для симуляції прибуття
        console.log(`🧪 Testing arrival for delivery ${deliveryId}`);

        const deliveryIdStr = String(deliveryId);
        if (deliveryData[deliveryIdStr]) {
            const delivery = deliveryData[deliveryIdStr];

            // Симулюємо оновлення позиції до координат магазину
            setDeliveryData(prev => ({
                ...prev,
                [deliveryIdStr]: {
                    ...delivery,
                    currentLatitude: delivery.storeLatitude,
                    currentLongitude: delivery.storeLongitude,
                    lastLocationUpdate: new Date().toISOString()
                }
            }));

            // Через 2 секунди змінюємо статус на Delivered
            setTimeout(() => {
                setDeliveryData(prev => {
                    const updated = { ...prev };
                    delete updated[deliveryIdStr];
                    return updated;
                });
            }, 5000);
        }
    };

    // Глобальна функція для тестування
    (window as any).testArrival = testArrival;

    return (
        <div className="app">
            {/* Map */}            <div id="map">                <MapComponent
                onLocationSelect={handleLocationSelect}
                deliveries={deliveryData}
                onShowVendorProducts={handleShowVendorProducts}
                onShowStoreInventory={handleShowStoreInventory}
                onCreateDelivery={handleCreateDelivery}
                onDeliveryArrived={handleDeliveryArrived}
                isAddingMode={isAddingMode}
                addingType={addingType}
                refreshTrigger={refreshTrigger}
            />
            </div>{/* Control buttons */}
            <div className="control-buttons">
                <button onClick={handleAddVendor}>
                    🏭 {isAddingMode && addingType === 'vendor' ? 'Click on map to add vendor' : 'Add Vendor'}
                </button>
                <button onClick={handleAddStore}>
                    🏪 {isAddingMode && addingType === 'store' ? 'Click on map to add store' : 'Add Store'}
                </button>
                {isAddingMode && (
                    <button className='control-buttons--cancel'
                        onClick={handleCancelAdd}
                        style={{ backgroundColor: '#dc3545', color: '#fff' }}
                    >
                        ❌ Cancel
                    </button>
                )}
            </div>            {/* Delivery info panel */}
            <div className="delivery-info">

                <div className={`connection-status ${connectionStatus}`}>
                    SignalR: {connectionStatus === 'connected' ? '🟢 Connected' :
                        connectionStatus === 'disconnected' ? '🔴 Disconnected' :
                            '⚠️ Error'}
                </div>

                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                    <h3 style={{
                        fontSize: '14px',
                        marginBottom: '0'
                    }}>📦 Active Deliveries</h3>
                    <button
                        onClick={() => setShowAllDeliveriesModal(true)}
                        className='all-deliveries-button'
                    >
                        📋 All Deliveries
                    </button>
                </div>

                {Object.keys(deliveryData).length === 0 ? (
                    <p>No active deliveries</p>
                ) : (
                    <div>
                        {Object.entries(deliveryData).map(([deliveryId, delivery]) => (<div key={deliveryId} className="delivery-item">
                            <h4>🚛 Delivery #{deliveryId}</h4>
                            <p><strong>Status:</strong> {getStatusText(delivery.status)}</p>
                            <p><strong>Driver:</strong> {delivery.driverId || 'Not assigned'}</p>
                            {delivery.currentLatitude && delivery.currentLongitude && (
                                <p><strong>Location:</strong> {delivery.currentLatitude.toFixed(4)}, {delivery.currentLongitude.toFixed(4)}</p>
                            )}
                            {delivery.lastLocationUpdate && (
                                <p><strong>Last Update:</strong> {new Date(delivery.lastLocationUpdate).toLocaleTimeString()}</p>
                            )}
                            {delivery.totalAmount && (
                                <p><strong>Total:</strong> ${delivery.totalAmount.toFixed(2)}</p>
                            )}                            <div style={{ display: 'flex', gap: '5px', marginTop: '10px' }}>
                                <button
                                    className='save-button'
                                    onClick={() => {
                                        setSelectedDeliveryId(parseInt(deliveryId));
                                        setShowDeliveryProductsModal(true);
                                    }}
                                    style={{
                                        padding: '4px 8px',
                                        backgroundColor: '#28a745',
                                        color: 'white',
                                        border: 'none',
                                        borderRadius: '3px',
                                        fontSize: '12px'
                                    }}
                                >
                                    📦 Products
                                </button>
                            </div>
                        </div>
                        ))}
                    </div>
                )}
            </div><AddLocationModal
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

            <AllDeliveriesModal
                isOpen={showAllDeliveriesModal}
                onClose={() => setShowAllDeliveriesModal(false)}
                onShowProducts={(deliveryId) => {
                    setSelectedDeliveryId(deliveryId);
                    setShowDeliveryProductsModal(true);
                }}
            />            {/* Delivery Products Modal */}
            <DeliveryProductsModal
                isOpen={showDeliveryProductsModal}
                deliveryId={selectedDeliveryId}
                onClose={() => {
                    setShowDeliveryProductsModal(false);
                    setSelectedDeliveryId(null);
                }}
            />
        </div>
    );
};

const App: React.FC = () => {
    return (
        <ConfirmationProvider>
            <ToastProvider>
                <AppContent />
            </ToastProvider>
        </ConfirmationProvider>
    );
};

export default App;
