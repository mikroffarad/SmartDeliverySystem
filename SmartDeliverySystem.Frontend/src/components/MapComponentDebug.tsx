import React, { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { deliveryApi } from '../services/deliveryApi';
import { VendorData, StoreData, DeliveryData } from '../types/delivery';
import { getStatusText } from '../utils/deliveryUtils';
import { useToast } from '../contexts/ToastContext';
import { useConfirmation } from '../contexts/ConfirmationContext';

// Fix for default markers
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
    iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
    shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

interface MapComponentProps {
    onLocationSelect?: (lat: number, lng: number) => void;
    deliveries: Record<string, DeliveryData>;
    onShowVendorProducts?: (vendorId: number) => void;
    onShowStoreInventory?: (storeId: number, storeName?: string) => void;
    onCreateDelivery?: (vendorId: number) => void;
    onDeliveryArrived?: (deliveryId: string) => void; // Новий колбек для прибуття
    isAddingMode?: boolean;
    addingType?: 'vendor' | 'store' | null;
    refreshTrigger?: number;
}

export const MapComponent: React.FC<MapComponentProps> = ({
    onLocationSelect,
    deliveries,
    onShowVendorProducts,
    onShowStoreInventory,
    onCreateDelivery,
    onDeliveryArrived,
    isAddingMode = false,
    refreshTrigger
}) => {
    const { showSuccess, showError } = useToast();
    const { showConfirmation } = useConfirmation();
    const mapRef = useRef<L.Map | null>(null);
    const mapContainer = useRef<HTMLDivElement>(null);
    const [vendors, setVendors] = useState<VendorData[]>([]);
    const [stores, setStores] = useState<StoreData[]>([]);

    const deliveryMarkersRef = useRef<Record<string, L.Marker>>({});
    const deliveryRoutesRef = useRef<Record<string, L.Polyline>>({});
    const arrivedDeliveriesRef = useRef<Set<string>>(new Set()); // Флаг для прибулих доставок
    const vendorMarkersRef = useRef<L.Marker[]>([]);
    const storeMarkersRef = useRef<L.Marker[]>([]);    // Create truck icon factory - memoized to prevent recreation
    const createTruckIcon = useCallback(() => {
        console.log('🏗️ Creating truck icon');
        try {
            const icon = L.icon({
                iconUrl: 'src/images/truck128.png',
                iconSize: [32, 32],
                iconAnchor: [16, 16],
                popupAnchor: [0, -16]
            });
            console.log('✅ Truck icon created successfully:', icon);
            return icon;
        } catch (error) {
            console.error('❌ Error creating truck icon:', error);
            return new L.Icon.Default();
        }
    }, []);    // Get route from OSRM API
    const getRouteFromOSRM = useCallback(async (fromLat: number, fromLon: number, toLat: number, toLon: number) => {
        try {
            console.log('🗺️ Getting route from OSRM API');
            // Use toFixed to ensure proper decimal formatting with dot separator
            const url = `http://router.project-osrm.org/route/v1/driving/${fromLon.toFixed(6)},${fromLat.toFixed(6)};${toLon.toFixed(6)},${toLat.toFixed(6)}?overview=full&geometries=geojson`;

            console.log('🌐 OSRM URL:', url);

            const response = await fetch(url);
            if (!response.ok) {
                console.warn('OSRM API request failed:', response.status);
                const errorText = await response.text();
                console.warn('OSRM Error:', errorText);
                return null;
            }

            const data = await response.json();
            if (!data.routes || data.routes.length === 0) {
                console.warn('OSRM returned empty route');
                return null;
            }

            const coordinates = data.routes[0].geometry.coordinates;
            const routePoints = coordinates.map((coord: number[]) => [coord[1], coord[0]] as [number, number]);

            console.log('✅ Route obtained with', routePoints.length, 'points');
            return routePoints;
        } catch (error) {
            console.error('❌ Error getting route from OSRM:', error);
            return null;
        }
    }, []);    // Create or update delivery route
    const updateDeliveryRoute = useCallback(async (deliveryId: string, delivery: DeliveryData) => {
        if (!mapRef.current) return;

        console.log('🛣️ Updating route for delivery', deliveryId);
        console.log('🛣️ Delivery data:', delivery);

        // Find vendor and store coordinates
        const vendor = vendors.find(v => v.id === delivery.vendorId);
        const store = stores.find(s => s.id === delivery.storeId);

        console.log('🛣️ Found vendor:', vendor);
        console.log('🛣️ Found store:', store);
        console.log('🛣️ Available vendors:', vendors.map(v => ({ id: v.id, name: v.name })));
        console.log('🛣️ Available stores:', stores.map(s => ({ id: s.id, name: s.name })));

        if (!vendor || !store || !vendor.latitude || !vendor.longitude || !store.latitude || !store.longitude) {
            console.log('❌ Missing vendor or store coordinates for delivery', deliveryId);
            console.log('❌ Vendor found:', !!vendor, 'Store found:', !!store);
            if (vendor) console.log('❌ Vendor coordinates:', vendor.latitude, vendor.longitude);
            if (store) console.log('❌ Store coordinates:', store.latitude, store.longitude);
            return;
        }

        // Remove existing route
        const existingRoute = deliveryRoutesRef.current[deliveryId];
        if (existingRoute) {
            mapRef.current.removeLayer(existingRoute);
            delete deliveryRoutesRef.current[deliveryId];
        }

        // Get route from OSRM
        const routePoints = await getRouteFromOSRM(
            vendor.latitude, vendor.longitude,
            store.latitude, store.longitude
        );

        if (routePoints) {
            // Create polyline for the route
            const route = L.polyline(routePoints, {
                color: '#007bff',
                weight: 4,
                opacity: 0.7,
                dashArray: '5, 5' // Dashed line to distinguish from other lines
            }).addTo(mapRef.current);

            // Add route to ref
            deliveryRoutesRef.current[deliveryId] = route;
            console.log('✅ Route created for delivery', deliveryId);
        } else {
            // Fallback: create simple straight line
            const straightLine = L.polyline([
                [vendor.latitude, vendor.longitude],
                [store.latitude, store.longitude]
            ], {
                color: '#dc3545',
                weight: 2,
                opacity: 0.5,
                dashArray: '10, 10'
            }).addTo(mapRef.current);

            deliveryRoutesRef.current[deliveryId] = straightLine;
            console.log('⚠️ Fallback straight line created for delivery', deliveryId);
        }
    }, [vendors, stores, getRouteFromOSRM]);// Initialize map only once
    useEffect(() => {
        console.log('🗺️ Map initialization useEffect called');
        console.log('🗺️ mapContainer.current:', mapContainer.current);
        console.log('🗺️ mapRef.current:', mapRef.current);
        console.log('🗺️ onLocationSelect dependency:', onLocationSelect);

        if (!mapContainer.current || mapRef.current) {
            console.log('🗺️ Skipping map initialization - container missing or map already exists');
            return;
        }

        console.log('🗺️ Initializing map...');
        const map = L.map(mapContainer.current).setView([48.3794, 31.1656], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map); mapRef.current = map;
        console.log('✅ Map initialized successfully');
        loadVendorsAndStores();

        return () => {
            console.log('🗺️ Cleaning up map...');
            if (mapRef.current) {
                mapRef.current.remove();
                mapRef.current = null;
            }
        };
    }, []); // EMPTY DEPENDENCY ARRAY - map should initialize only once

    // Handle map click events separately to avoid map recreation
    useEffect(() => {
        if (!mapRef.current) return;

        console.log('🗺️ Setting up map click handler');

        const handleMapClick = (e: L.LeafletMouseEvent) => {
            console.log('🗺️ Map click event triggered');
            console.log('🗺️ Current onLocationSelect function:', onLocationSelect);
            if (onLocationSelect) {
                onLocationSelect(e.latlng.lat, e.latlng.lng);
            }
        };

        mapRef.current.on('click', handleMapClick);

        return () => {
            if (mapRef.current) {
                mapRef.current.off('click', handleMapClick);
            }
        };
    }, [onLocationSelect]);    // Load vendors and stores
    const loadVendorsAndStores = async () => {
        try {
            console.log('🔄 Loading vendors and stores...');
            const [vendorsData, storesData] = await Promise.all([
                deliveryApi.getAllVendors(),
                deliveryApi.getAllStores()
            ]);
            console.log('✅ Loaded vendors:', vendorsData);
            console.log('✅ Loaded stores:', storesData);
            setVendors(vendorsData);
            setStores(storesData);
        } catch (error) {
            console.error('❌ Error loading vendors and stores:', error);
        }
    };

    // Reload vendors and stores when refresh trigger changes
    useEffect(() => {
        if (refreshTrigger !== undefined) {
            loadVendorsAndStores();
        }
    }, [refreshTrigger]);

    // Update cursor for adding mode
    useEffect(() => {
        if (mapRef.current) {
            const container = mapRef.current.getContainer();
            container.style.cursor = isAddingMode ? 'crosshair' : '';
        }
    }, [isAddingMode]);

    // Handle vendors
    useEffect(() => {
        if (!mapRef.current) return;

        // Clear existing vendor markers
        vendorMarkersRef.current.forEach(marker => {
            mapRef.current?.removeLayer(marker);
        });
        vendorMarkersRef.current = [];

        // Add vendor markers
        vendors.forEach(vendor => {
            if (vendor.latitude && vendor.longitude) {
                const vendorIcon = L.icon({
                    iconUrl: 'src/images/vendor128.png',
                    iconSize: [28, 28],
                    iconAnchor: [14, 28],
                    popupAnchor: [0, -28]
                });

                const marker = L.marker([vendor.latitude, vendor.longitude], { icon: vendorIcon })
                    .addTo(mapRef.current!)
                    .bindPopup(`
                        <div>
                            <b>🏭 ${vendor.name}</b><br>
                            Vendor<br>
                            <button onclick="window.showVendorProducts(${vendor.id})"
                                    style="background: #28a745; color: white; border: none; padding: 5px 10px; margin: 2px; border-radius: 3px; display: block; width: 100%;">
                                📦 Products
                            </button>
                            <button onclick="window.createDelivery(${vendor.id})"
                                    style="background: #007bff; color: white; border: none; padding: 5px 10px; margin: 2px; border-radius: 3px; display: block; width: 100%;">
                                🚛 Create Delivery
                            </button>
                            <button onclick="window.deleteVendor(${vendor.id})"
                                    style="background: #dc3545; color: white; border: none; padding: 5px 10px; margin: 2px; border-radius: 3px; display: block; width: 100%;">
                                🗑️ Delete
                            </button>
                        </div>
                    `);
                vendorMarkersRef.current.push(marker);
            }
        });

        (window as any).showVendorProducts = (vendorId: number) => {
            if (onShowVendorProducts) onShowVendorProducts(vendorId);
        };
        (window as any).createDelivery = (vendorId: number) => {
            if (onCreateDelivery) onCreateDelivery(vendorId);
        }; (window as any).deleteVendor = async (vendorId: number) => {
            const confirmed = await showConfirmation({
                title: 'Delete Vendor',
                message: 'Are you sure you want to delete this vendor? This action cannot be undone.',
                confirmText: 'Delete',
                cancelText: 'Cancel',
                confirmColor: '#dc3545'
            });

            if (!confirmed) return;

            try {
                await deliveryApi.deleteVendor(vendorId);
                showSuccess('Vendor deleted successfully');
                loadVendorsAndStores();
            } catch (error) {
                console.error('Error deleting vendor:', error);
                const errorMessage = error instanceof Error ? error.message :
                    (typeof error === 'string' ? error : 'Unknown error occurred');
                showError(`Error deleting vendor: ${errorMessage}`);
            }
        };
    }, [vendors, onShowVendorProducts, onCreateDelivery]);

    // Handle stores
    useEffect(() => {
        if (!mapRef.current) return;

        // Clear existing store markers
        storeMarkersRef.current.forEach(marker => {
            mapRef.current?.removeLayer(marker);
        });
        storeMarkersRef.current = [];

        // Add store markers
        stores.forEach(store => {
            if (store.latitude && store.longitude) {
                const storeIcon = L.icon({
                    iconUrl: 'src/images/store128.png',
                    iconSize: [28, 28],
                    iconAnchor: [14, 28],
                    popupAnchor: [0, -28]
                });

                const marker = L.marker([store.latitude, store.longitude], { icon: storeIcon })
                    .addTo(mapRef.current!)
                    .bindPopup(`
                        <div>
                            <b>🏪 ${store.name}</b><br>
                            Store<br>
                            <button onclick="window.showStoreInventory(${store.id}, '${store.name}')"
                                    style="background: #17a2b8; color: white; border: none; padding: 5px 10px; margin: 2px; border-radius: 3px; display: block; width: 100%;">
                                📋 Inventory
                            </button>
                            <button onclick="window.deleteStore(${store.id})"
                                    style="background: #dc3545; color: white; border: none; padding: 5px 10px; margin: 2px; border-radius: 3px; display: block; width: 100%;">
                                🗑️ Delete
                            </button>
                        </div>
                    `);
                storeMarkersRef.current.push(marker);
            }
        });

        (window as any).showStoreInventory = (storeId: number, storeName?: string) => {
            if (onShowStoreInventory) onShowStoreInventory(storeId, storeName);
        }; (window as any).deleteStore = async (storeId: number) => {
            const confirmed = await showConfirmation({
                title: 'Delete Store',
                message: 'Are you sure you want to delete this store? This action cannot be undone.',
                confirmText: 'Delete',
                cancelText: 'Cancel',
                confirmColor: '#dc3545'
            });

            if (!confirmed) return;

            try {
                await deliveryApi.deleteStore(storeId);
                showSuccess('Store deleted successfully');
                loadVendorsAndStores();
            } catch (error) {
                console.error('Error deleting store:', error);
                const errorMessage = error instanceof Error ? error.message :
                    (typeof error === 'string' ? error : 'Unknown error occurred');
                showError(`Error deleting store: ${errorMessage}`);
            }
        };
    }, [stores, onShowStoreInventory]);    // Memoize delivery keys for comparison with coordinates
    const deliveryKeys = useMemo(() => {
        return Object.entries(deliveries)
            .map(([id, delivery]) => `${id}:${delivery.currentLatitude}:${delivery.currentLongitude}`)
            .sort()
            .join(',');
    }, [deliveries]);// Handle delivery markers with ULTRA DETAILED LOGGING
    useEffect(() => {
        if (!mapRef.current) {
            console.log('❌ Map not ready, skipping delivery markers update');
            return;
        } console.log('🚛 === DELIVERY MARKERS UPDATE ===');
        const updateTime = new Date().toLocaleTimeString();
        console.log(`🚛 Update time: ${updateTime}`);
        console.log('🚛 Received deliveries:', Object.keys(deliveries));
        console.log('🚛 Current markers:', Object.keys(deliveryMarkersRef.current));
        console.log('🚛 Map instance:', mapRef.current);        // Process each delivery
        Object.entries(deliveries).forEach(([deliveryId, delivery]) => {
            console.log(`🚛 Processing delivery ${deliveryId}:`, delivery);

            // Skip if delivery has already arrived
            if (arrivedDeliveriesRef.current.has(deliveryId)) {
                console.log(`🚛 ⏭️ Skipping delivery ${deliveryId} - already arrived`);
                return;
            }

            if (!delivery.currentLatitude || !delivery.currentLongitude) {
                console.log(`🚛 ❌ Delivery ${deliveryId} has no coordinates`);
                return;
            }// Update or create route for this delivery ONLY ONCE
            if (!deliveryRoutesRef.current[deliveryId]) {
                console.log(`🛣️ Creating route for delivery ${deliveryId} for the first time`);
                updateDeliveryRoute(deliveryId, delivery);
            } else {
                console.log(`🛣️ Route already exists for delivery ${deliveryId}, skipping`);
            }

            const existingMarker = deliveryMarkersRef.current[deliveryId];
            console.log(`🚛 Existing marker for ${deliveryId}:`, existingMarker ? 'EXISTS' : 'NOT EXISTS'); if (existingMarker) {
                // Check if truck has arrived at destination
                if (delivery.vendorLatitude && delivery.vendorLongitude &&
                    delivery.storeLatitude && delivery.storeLongitude) {

                    const latDiff = Math.abs(delivery.storeLatitude - delivery.currentLatitude);
                    const lngDiff = Math.abs(delivery.storeLongitude - delivery.currentLongitude);

                    // Check arrival conditions with enhanced logging
                    const isAtDestination = latDiff < 0.0005 && lngDiff < 0.0005; // Збільшено поріг
                    const isDelivered = delivery.status === 4 || delivery.status === 'Delivered';
                    const hasArrivalMessage = delivery.lastLocationUpdate &&
                        String(delivery.lastLocationUpdate).includes('Прибуття'); console.log(`🎯 Delivery ${deliveryId} arrival check:`, {
                            isAtDestination,
                            isDelivered,
                            hasArrivalMessage,
                            latDiff,
                            lngDiff,
                            status: delivery.status,
                            lastUpdate: delivery.lastLocationUpdate
                        });                    // СПРОЩЕНА УМОВА: якщо вантажівка на місці призначення - показуємо прибуття
                    if (isAtDestination && !arrivedDeliveriesRef.current.has(deliveryId)) {
                        console.log(`🎯 Delivery ${deliveryId} has arrived at destination!`);

                        // Mark delivery as arrived to prevent recreation
                        arrivedDeliveriesRef.current.add(deliveryId);
                        console.log(`🏁 Marked delivery ${deliveryId} as arrived`);

                        // Автоматично оновлюємо статус доставки на "Delivered"
                        try {
                            console.log(`🔄 Updating delivery ${deliveryId} status to Delivered`);
                            // Викликаємо API для оновлення статусу
                            fetch(`https://localhost:7183/api/delivery/${deliveryId}/status`, {
                                method: 'PUT',
                                headers: {
                                    'Content-Type': 'application/json',
                                },
                                body: JSON.stringify(4) // 4 = Delivered
                            }).then(response => {
                                if (response.ok) {
                                    console.log(`✅ Successfully updated delivery ${deliveryId} to Delivered`);
                                } else {
                                    console.error(`❌ Failed to update delivery ${deliveryId} status`);
                                }
                            });
                        } catch (error) {
                            console.error(`❌ Error updating delivery ${deliveryId} status:`, error);
                        }                        // Show arrival notification and keep truck visible for 2 seconds
                        existingMarker.getPopup()?.setContent(`
                            <div style="text-align: center; padding: 10px;">
                                <b>🚛 Delivery #${deliveryId}</b><br>
                                Driver: ${delivery.driverId || 'Not assigned'}<br>
                                <strong style="color: green; font-size: 16px;">🎯 ARRIVED AT DESTINATION!</strong><br>
                                <strong style="color: green;">📦 Marking as Delivered...</strong><br>
                                Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                                Updated: ${updateTime}
                            </div>
                        `);

                        // Auto-open popup to show arrival
                        existingMarker.openPopup();

                        // Notify parent component about arrival
                        if (onDeliveryArrived) {
                            onDeliveryArrived(deliveryId);
                        }                        // Remove truck and route after exactly 5 seconds to give user time to see arrival
                        setTimeout(() => {
                            console.log(`🗑️ Removing delivery ${deliveryId} after arrival display`);

                            // Remove truck marker
                            if (deliveryMarkersRef.current[deliveryId]) {
                                mapRef.current?.removeLayer(deliveryMarkersRef.current[deliveryId]);
                                delete deliveryMarkersRef.current[deliveryId];
                                console.log(`🚛 ✅ Removed truck marker for delivery ${deliveryId}`);
                            }

                            // Remove route more forcefully
                            if (deliveryRoutesRef.current[deliveryId]) {
                                const route = deliveryRoutesRef.current[deliveryId];
                                if (mapRef.current?.hasLayer(route)) {
                                    mapRef.current.removeLayer(route);
                                    console.log(`🛣️ ✅ Removed route layer for delivery ${deliveryId}`);
                                }
                                delete deliveryRoutesRef.current[deliveryId];
                                console.log(`🛣️ ✅ Deleted route reference for delivery ${deliveryId}`);
                            }

                            // Force remove ALL polylines that might be related to this delivery
                            let removedCount = 0;
                            mapRef.current?.eachLayer((layer) => {
                                if (layer instanceof L.Polyline) {
                                    // Remove any polyline that looks like a delivery route
                                    const options = (layer as any).options;
                                    if (options.dashArray === '5, 5' || options.dashArray === '10, 10') {
                                        mapRef.current?.removeLayer(layer);
                                        removedCount++;
                                        console.log(`🛣️ ✅ Force removed polyline`);
                                    }
                                }
                            }); if (removedCount > 0) {
                                console.log(`🛣️ ✅ Force removed ${removedCount} additional polylines`);
                            }
                        }, 3000); // 5 секунд для синхронізації з бекендом

                        return; // Don't continue with normal update
                    }
                }

                // Update existing marker
                console.log(`🚛 ➡️ Updating position for delivery ${deliveryId} @ ${updateTime}`);
                console.log(`🚛 New coordinates: [${delivery.currentLatitude}, ${delivery.currentLongitude}]`);

                try {
                    existingMarker.setLatLng([delivery.currentLatitude, delivery.currentLongitude]);
                    console.log(`🚛 ✅ Successfully updated position for delivery ${deliveryId}`);
                } catch (error) {
                    console.error(`🚛 ❌ Error updating position for delivery ${deliveryId}:`, error);
                }                // Update popup with current time
                try {
                    existingMarker.getPopup()?.setContent(`
                        <div>
                            <b>🚛 Delivery #${deliveryId}</b><br>
                            Driver: ${delivery.driverId || 'Not assigned'}<br>
                            Status: ${getStatusText(delivery.status)}<br>
                            Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                            Updated: ${updateTime}<br>
                        </div>
                    `);
                } catch (error) {
                    console.error(`🚛 ❌ Error updating popup for delivery ${deliveryId}:`, error);
                }
            } else {
                // Create new marker
                console.log(`🚛 🆕 Creating NEW marker for delivery ${deliveryId}`);
                console.log(`🚛 Coordinates: [${delivery.currentLatitude}, ${delivery.currentLongitude}]`);

                try {
                    const truckIcon = createTruckIcon();
                    console.log(`🚛 Created truck icon:`, truckIcon);

                    const marker = L.marker([delivery.currentLatitude, delivery.currentLongitude], { icon: truckIcon });
                    console.log(`🚛 Created marker instance:`, marker);

                    marker.addTo(mapRef.current!);
                    console.log(`🚛 Added marker to map`); marker.bindPopup(`
                        <div>
                            <b>🚛 Delivery #${deliveryId}</b><br>
                            Driver: ${delivery.driverId || 'Not assigned'}<br>
                            Status: ${getStatusText(delivery.status)}<br>
                            Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                            Created: ${new Date().toLocaleTimeString()}<br>
                        </div>
                    `);
                    console.log(`🚛 Bound popup to marker`);

                    deliveryMarkersRef.current[deliveryId] = marker;
                    console.log(`🚛 ✅ Stored marker for delivery ${deliveryId}`);
                    console.log(`🚛 Total delivery markers now:`, Object.keys(deliveryMarkersRef.current).length);

                    // Verify marker is on map
                    if (mapRef.current?.hasLayer(marker)) {
                        console.log(`🚛 ✅ Confirmed marker ${deliveryId} is on map`);
                    } else {
                        console.log(`🚛 ❌ Marker ${deliveryId} NOT found on map!`);
                    }
                } catch (error) {
                    console.error(`🚛 ❌ Error creating marker for delivery ${deliveryId}:`, error);
                }
            }
        });

        // Remove markers for deliveries that no longer exist
        const currentDeliveryIds = Object.keys(deliveries);
        const existingMarkerIds = Object.keys(deliveryMarkersRef.current); existingMarkerIds.forEach(deliveryId => {
            if (!currentDeliveryIds.includes(deliveryId)) {
                // Don't remove if delivery has arrived (let the timeout handle it)
                if (arrivedDeliveriesRef.current.has(deliveryId)) {
                    console.log(`🚛 ⏳ Delivery ${deliveryId} has arrived, letting timeout handle removal`);
                    return;
                }

                console.log(`🚛 🗑️ Removing marker for delivery ${deliveryId} (no longer exists)`);
                const marker = deliveryMarkersRef.current[deliveryId];
                const route = deliveryRoutesRef.current[deliveryId];

                if (marker) {
                    mapRef.current?.removeLayer(marker);
                    delete deliveryMarkersRef.current[deliveryId];
                    console.log(`🚛 ✅ Removed marker for delivery ${deliveryId}`);
                }

                if (route) {
                    mapRef.current?.removeLayer(route);
                    delete deliveryRoutesRef.current[deliveryId];
                    console.log(`🚛 ✅ Removed route for delivery ${deliveryId}`);
                }

                // Also remove from arrived deliveries set
                arrivedDeliveriesRef.current.delete(deliveryId);
                console.log(`🏁 Removed delivery ${deliveryId} from arrived set`);
            }
        }); console.log('🚛 === END DELIVERY MARKERS UPDATE ===');

    }, [deliveryKeys, createTruckIcon, updateDeliveryRoute]);// Глобальна функція для очищення всіх маршрутів (для тестування)
    (window as any).clearAllRoutes = () => {
        console.log('🧹 Clearing all routes manually');
        if (mapRef.current) {
            mapRef.current.eachLayer((layer) => {
                if (layer instanceof L.Polyline) {
                    mapRef.current?.removeLayer(layer);
                    console.log('🛣️ Removed polyline layer');
                }
            });
        }
        // Clear refs
        Object.keys(deliveryRoutesRef.current).forEach(deliveryId => {
            delete deliveryRoutesRef.current[deliveryId];
        });
        // Clear arrived deliveries too
        arrivedDeliveriesRef.current.clear();
        console.log('✅ All routes and arrived flags cleared');
    };

    return <div ref={mapContainer} style={{ height: '100%', width: '100%' }} />;
};
