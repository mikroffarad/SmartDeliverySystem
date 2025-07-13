import React, { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { deliveryApi } from '../services/deliveryApi';
import { VendorData, StoreData, DeliveryData } from '../types/delivery';

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
    onMarkAsDelivered?: (deliveryId: number) => void;
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
    onMarkAsDelivered,
    isAddingMode = false,
    addingType = null,
    refreshTrigger
}) => {
    const mapRef = useRef<L.Map | null>(null);
    const mapContainer = useRef<HTMLDivElement>(null);
    const [vendors, setVendors] = useState<VendorData[]>([]);
    const [stores, setStores] = useState<StoreData[]>([]);

    const deliveryMarkersRef = useRef<Record<string, L.Marker>>({});
    const vendorMarkersRef = useRef<L.Marker[]>([]);
    const storeMarkersRef = useRef<L.Marker[]>([]);    // Initialize map only once
    useEffect(() => {
        if (!mapContainer.current || mapRef.current) return;

        const map = L.map(mapContainer.current).setView([48.3794, 31.1656], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map); map.on('click', (e) => {
            console.log('🗺️ Map clicked at:', e.latlng.lat, e.latlng.lng);
            console.log('🗺️ onLocationSelect function:', onLocationSelect);
            console.log('🗺️ isAddingMode:', isAddingMode);

            if (onLocationSelect) {
                console.log('🗺️ Calling onLocationSelect...');
                onLocationSelect(e.latlng.lat, e.latlng.lng);
            } else {
                console.log('🗺️ onLocationSelect is not defined');
            }
        });

        mapRef.current = map;
        loadVendorsAndStores();

        return () => {
            if (mapRef.current) {
                mapRef.current.remove();
                mapRef.current = null;
            }
        };
    }, [onLocationSelect]); // Add onLocationSelect as dependency

    // Load vendors and stores
    const loadVendorsAndStores = async () => {
        try {
            const [vendorsData, storesData] = await Promise.all([
                deliveryApi.getAllVendors(),
                deliveryApi.getAllStores()
            ]);
            setVendors(vendorsData);
            setStores(storesData);
        } catch (error) {
            console.error('Error loading vendors and stores:', error);
        }
    };

    // Reload vendors and stores when refresh trigger changes
    useEffect(() => {
        if (refreshTrigger !== undefined) {
            loadVendorsAndStores();
        }
    }, [refreshTrigger]);    // Update cursor for adding mode
    useEffect(() => {
        if (mapRef.current) {
            const container = mapRef.current.getContainer();
            container.style.cursor = isAddingMode ? 'crosshair' : '';
            console.log('🎯 Cursor updated. isAddingMode:', isAddingMode, 'cursor:', container.style.cursor);
        }
    }, [isAddingMode]);

    // Handle vendors
    useEffect(() => {
        if (!mapRef.current) return;

        // Clear existing vendor markers
        vendorMarkersRef.current.forEach(marker => {
            mapRef.current?.removeLayer(marker);
        });
        vendorMarkersRef.current = [];        // Add vendor markers
        vendors.forEach(vendor => {
            if (vendor.latitude && vendor.longitude) {
                // Create vendor icon with fallback
                let vendorIcon;
                try {
                    vendorIcon = L.icon({
                        iconUrl: '/src/images/vendor128.png',
                        iconSize: [32, 32],
                        iconAnchor: [16, 32],
                        popupAnchor: [0, -32]
                    });
                } catch (error) {
                    // Fallback to default blue marker
                    vendorIcon = new L.Icon.Default();
                }

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
        });        // Global functions
        (window as any).showVendorProducts = (vendorId: number) => {
            if (onShowVendorProducts) onShowVendorProducts(vendorId);
        };
        (window as any).createDelivery = (vendorId: number) => {
            if (onCreateDelivery) onCreateDelivery(vendorId);
        };
        (window as any).deleteVendor = async (vendorId: number) => {
            if (confirm('Are you sure you want to delete this vendor?')) {
                try {
                    await deliveryApi.deleteVendor(vendorId);
                    loadVendorsAndStores();
                } catch (error) {
                    alert('Error deleting vendor');
                }
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
        storeMarkersRef.current = [];        // Add store markers
        stores.forEach(store => {
            if (store.latitude && store.longitude) {
                // Create store icon with fallback
                let storeIcon;
                try {
                    storeIcon = L.icon({
                        iconUrl: '/src/images/store128.png',
                        iconSize: [32, 32],
                        iconAnchor: [16, 32],
                        popupAnchor: [0, -32]
                    });
                } catch (error) {
                    // Fallback to default marker
                    storeIcon = new L.Icon.Default();
                }

                const marker = L.marker([store.latitude, store.longitude], { icon: storeIcon })
                    .addTo(mapRef.current!)
                    .bindPopup(`
                        <div>
                            <b>🏪 ${store.name}</b><br>
                            Store<br>
                            <button onclick="window.showStoreInventory(${store.id})"
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
        }); (window as any).showStoreInventory = (storeId: number, storeName?: string) => {
            if (onShowStoreInventory) onShowStoreInventory(storeId, storeName);
        };
        (window as any).deleteStore = async (storeId: number) => {
            if (confirm('Are you sure you want to delete this store?')) {
                try {
                    await deliveryApi.deleteStore(storeId);
                    loadVendorsAndStores();
                } catch (error) {
                    alert('Error deleting store');
                }
            }
        };
    }, [stores, onShowStoreInventory]);    // Handle delivery markers - OPTIMIZED VERSION
    useEffect(() => {
        if (!mapRef.current) return;

        console.log('📍 Updating delivery markers:', Object.keys(deliveries));

        const currentDeliveryIds = Object.keys(deliveries);
        const existingMarkerIds = Object.keys(deliveryMarkersRef.current);

        // 1. Remove markers for deliveries that no longer exist
        existingMarkerIds.forEach(deliveryId => {
            if (!currentDeliveryIds.includes(deliveryId)) {
                console.log(`📍 Removing marker for delivery ${deliveryId} (no longer exists)`);
                const marker = deliveryMarkersRef.current[deliveryId];
                if (marker) {
                    mapRef.current?.removeLayer(marker);
                    delete deliveryMarkersRef.current[deliveryId];
                }
            }
        });

        // 2. Add markers for new deliveries only
        currentDeliveryIds.forEach(deliveryId => {
            const delivery = deliveries[deliveryId];

            if (delivery.currentLatitude && delivery.currentLongitude) {
                const existingMarker = deliveryMarkersRef.current[deliveryId];

                console.log(`📍 Existing marker for ${deliveryId}:`, existingMarker ? 'found' : 'not found');
                console.log(`📍 Map has marker layer:`, existingMarker ? mapRef.current!.hasLayer(existingMarker) : 'N/A');

                if (!existingMarker) {
                    // Create new marker only if it doesn't exist
                    console.log(`📍 Creating NEW marker for delivery ${deliveryId}`);

                    // Create truck icon with fallback
                    let truckIcon;
                    try {
                        truckIcon = L.icon({
                            iconUrl: '/src/images/truck128.png',
                            iconSize: [32, 32],
                            iconAnchor: [16, 16],
                            popupAnchor: [0, -16]
                        });
                    } catch (error) {
                        truckIcon = new L.Icon.Default();
                    }

                    const marker = L.marker([delivery.currentLatitude, delivery.currentLongitude], { icon: truckIcon })
                        .addTo(mapRef.current!)
                        .bindPopup(`
                            <div>
                                <b>🚛 Delivery #${deliveryId}</b><br>
                                Driver: ${delivery.driverId || 'Not assigned'}<br>
                                Status: ${delivery.status}<br>
                                Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                                ${delivery.status === 'InTransit' ?
                                `<button onclick="window.markAsDelivered(${deliveryId})"
                                            style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 5px; border-radius: 3px; display: block; width: 100%;">
                                        ✅ Mark as Delivered
                                    </button>` : ''
                            }
                            </div>
                        `);

                    deliveryMarkersRef.current[deliveryId] = marker;
                } else {
                    // Just update position for existing marker
                    console.log(`📍 Updating EXISTING marker position for delivery ${deliveryId}`);

                    // Check if marker is still on the map
                    if (mapRef.current!.hasLayer(existingMarker)) {
                        existingMarker.setLatLng([delivery.currentLatitude, delivery.currentLongitude]);

                        // Update popup content
                        const popupContent = `
                            <div>
                                <b>🚛 Delivery #${deliveryId}</b><br>
                                Driver: ${delivery.driverId || 'Not assigned'}<br>
                                Status: ${delivery.status}<br>
                                Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                                ${delivery.status === 'InTransit' ?
                                `<button onclick="window.markAsDelivered(${deliveryId})"
                                            style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 5px; border-radius: 3px; display: block; width: 100%;">
                                        ✅ Mark as Delivered
                                    </button>` : ''
                            }
                            </div>
                        `;

                        if (existingMarker.getPopup()) {
                            existingMarker.getPopup()!.setContent(popupContent);
                        }
                    } else {
                        // Marker exists in ref but not on map - recreate it
                        console.log(`📍 Marker ${deliveryId} exists in ref but not on map, recreating...`);

                        // Remove from ref first
                        delete deliveryMarkersRef.current[deliveryId];

                        // Create new marker
                        let truckIcon;
                        try {
                            truckIcon = L.icon({
                                iconUrl: '/src/images/truck128.png',
                                iconSize: [32, 32],
                                iconAnchor: [16, 16],
                                popupAnchor: [0, -16]
                            });
                        } catch (error) {
                            truckIcon = new L.Icon.Default();
                        }

                        const marker = L.marker([delivery.currentLatitude, delivery.currentLongitude], { icon: truckIcon })
                            .addTo(mapRef.current!)
                            .bindPopup(`
                                <div>
                                    <b>🚛 Delivery #${deliveryId}</b><br>
                                    Driver: ${delivery.driverId || 'Not assigned'}<br>
                                    Status: ${delivery.status}<br>
                                    Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                                    ${delivery.status === 'InTransit' ?
                                    `<button onclick="window.markAsDelivered(${deliveryId})"
                                                style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 5px; border-radius: 3px; display: block; width: 100%;">
                                            ✅ Mark as Delivered
                                        </button>` : ''
                                }
                                </div>
                            `);

                        deliveryMarkersRef.current[deliveryId] = marker;
                        console.log(`✅ Recreated marker for delivery ${deliveryId}`);
                    }
                }
            }
        });

        (window as any).markAsDelivered = (deliveryId: number) => {
            if (onMarkAsDelivered) onMarkAsDelivered(deliveryId);
        };

    }, [deliveries, onMarkAsDelivered]);

    return <div ref={mapContainer} style={{ height: '100%', width: '100%' }} />;
};
