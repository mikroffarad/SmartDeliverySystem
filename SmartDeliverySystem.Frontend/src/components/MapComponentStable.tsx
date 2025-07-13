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
    refreshTrigger
}) => {
    const mapRef = useRef<L.Map | null>(null);
    const mapContainer = useRef<HTMLDivElement>(null);
    const [vendors, setVendors] = useState<VendorData[]>([]);
    const [stores, setStores] = useState<StoreData[]>([]);

    const deliveryMarkersRef = useRef<Record<string, L.Marker>>({});
    const vendorMarkersRef = useRef<L.Marker[]>([]);
    const storeMarkersRef = useRef<L.Marker[]>([]);

    // Initialize map only once
    useEffect(() => {
        if (!mapContainer.current || mapRef.current) return;

        const map = L.map(mapContainer.current).setView([48.3794, 31.1656], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map);

        map.on('click', (e) => {
            if (onLocationSelect) {
                onLocationSelect(e.latlng.lat, e.latlng.lng);
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
    }, [onLocationSelect]);

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
                    iconUrl: 'https://cdn-icons-png.flaticon.com/512/1076/1076471.png',
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
        storeMarkersRef.current = [];

        // Add store markers
        stores.forEach(store => {
            if (store.latitude && store.longitude) {
                const storeIcon = L.icon({
                    iconUrl: 'https://cdn-icons-png.flaticon.com/512/2830/2830284.png',
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
    }, [stores, onShowStoreInventory]);

    // Handle delivery markers - SIMPLE APPROACH LIKE INDEX.HTML
    useEffect(() => {
        if (!mapRef.current) return;

        console.log('📍 Processing deliveries update:', Object.keys(deliveries));

        // Update each delivery marker individually (like index.html does)
        Object.entries(deliveries).forEach(([deliveryId, delivery]) => {
            if (delivery.currentLatitude && delivery.currentLongitude) {
                const existingMarker = deliveryMarkersRef.current[deliveryId];

                if (existingMarker) {
                    // Just update position (like index.html setLatLng)
                    console.log(`📍 Updating position for delivery ${deliveryId}`);
                    existingMarker.setLatLng([delivery.currentLatitude, delivery.currentLongitude]);

                    // Update popup content
                    existingMarker.getPopup()?.setContent(`
                        <div>
                            <b>🚛 Delivery #${deliveryId}</b><br>
                            Driver: ${delivery.driverId || 'Not assigned'}<br>
                            Status: ${delivery.status}<br>
                            Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                            Updated: ${new Date().toLocaleTimeString()}<br>
                            ${delivery.status === 'InTransit' ?
                            `<button onclick="window.markAsDelivered(${deliveryId})"
                                        style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 5px; border-radius: 3px; display: block; width: 100%;">
                                    ✅ Mark as Delivered
                                </button>` : ''
                        }
                        </div>
                    `);
                } else {
                    // Create new marker (like index.html does in the else block)
                    console.log(`📍 Creating new marker for delivery ${deliveryId}`);

                    const truckIcon = L.icon({
                        iconUrl: 'https://cdn-icons-png.flaticon.com/512/1730/1730543.png',
                        iconSize: [32, 32],
                        iconAnchor: [16, 16],
                        popupAnchor: [0, -16]
                    });

                    const marker = L.marker([delivery.currentLatitude, delivery.currentLongitude], { icon: truckIcon })
                        .addTo(mapRef.current!)
                        .bindPopup(`
                            <div>
                                <b>🚛 Delivery #${deliveryId}</b><br>
                                Driver: ${delivery.driverId || 'Not assigned'}<br>
                                Status: ${delivery.status}<br>
                                Location: ${delivery.currentLatitude.toFixed(4)}, ${delivery.currentLongitude.toFixed(4)}<br>
                                Updated: ${new Date().toLocaleTimeString()}<br>
                                ${delivery.status === 'InTransit' ?
                                `<button onclick="window.markAsDelivered(${deliveryId})"
                                            style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 5px; border-radius: 3px; display: block; width: 100%;">
                                        ✅ Mark as Delivered
                                    </button>` : ''
                            }
                            </div>
                        `);

                    deliveryMarkersRef.current[deliveryId] = marker;
                    console.log(`✅ Created truck marker for delivery ${deliveryId}`);
                }
            }
        });

        // Remove markers for deliveries that no longer exist
        const currentDeliveryIds = Object.keys(deliveries);
        const existingMarkerIds = Object.keys(deliveryMarkersRef.current);

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

        (window as any).markAsDelivered = (deliveryId: number) => {
            if (onMarkAsDelivered) onMarkAsDelivered(deliveryId);
        };

    }, [deliveries, onMarkAsDelivered]);

    return <div ref={mapContainer} style={{ height: '100%', width: '100%' }} />;
};
