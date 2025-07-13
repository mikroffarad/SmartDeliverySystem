import React, { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { deliveryApi } from '../services/deliveryApi';
import { VendorData, StoreData, DeliveryData } from '../types/delivery';
import { useDeliveryPositions } from '../hooks/useDeliveryOptimization';

// Fix for default markers in Leaflet
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
    onShowStoreInventory?: (storeId: number) => void;
    onCreateDelivery?: (vendorId: number) => void;
    onMarkAsDelivered?: (deliveryId: number) => void;
    isAddingMode?: boolean;
    addingType?: 'vendor' | 'store' | null;
    refreshTrigger?: number;
}

export const MapComponent: React.FC<MapComponentProps> = React.memo(({
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
    const [stores, setStores] = useState<StoreData[]>([]); const vendorMarkersRef = useRef<L.Marker[]>([]);
    const storeMarkersRef = useRef<L.Marker[]>([]);
    const deliveryMarkersRef = useRef<Record<string, L.Marker>>({});

    // Use optimization hook
    const { hasPositionChanged } = useDeliveryPositions(deliveries);

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

    useEffect(() => {
        if (!mapRef.current) return;
        const mapContainer = mapRef.current.getContainer();
        mapContainer.style.cursor = isAddingMode ? 'crosshair' : '';
    }, [isAddingMode]);

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
    }; useEffect(() => {
        if (refreshTrigger !== undefined) {
            loadVendorsAndStores();
        }
    }, [refreshTrigger]);

    // Update vendor markers
    useEffect(() => {
        if (!mapRef.current) return;

        vendorMarkersRef.current.forEach(marker => {
            mapRef.current?.removeLayer(marker);
        });
        vendorMarkersRef.current = [];

        vendors.forEach(vendor => {
            if (vendor.latitude && vendor.longitude) {
                const vendorIcon = L.icon({
                    iconUrl: 'src/images/vendor128.png',
                    iconSize: [32, 32],
                    iconAnchor: [16, 32],
                    popupAnchor: [0, -32]
                });

                const marker = L.marker([vendor.latitude, vendor.longitude], { icon: vendorIcon })
                    .addTo(mapRef.current!)
                    .bindPopup(`
                        <div>
                            <b>🏭 ${vendor.name}</b><br>
                            Vendor<br>
                            Lat: ${vendor.latitude.toFixed(4)}<br>
                            Lon: ${vendor.longitude.toFixed(4)}<br>
                            <div style="margin-top: 10px;">
                                <button onclick="window.showVendorProducts(${vendor.id})"
                                        style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-right: 5px; margin-bottom: 10px; border-radius: 3px; cursor: pointer;">
                                    📦 Products
                                </button><br>
                                <button onclick="window.createDelivery(${vendor.id})"
                                        style="background: #007bff; color: white; border: none; padding: 5px 10px; margin-right: 5px; margin-bottom: 10px; border-radius: 3px; cursor: pointer;">
                                    🚛 Create Delivery
                                </button><br>
                                <button onclick="window.deleteVendor(${vendor.id})"
                                        style="background: #dc3545; color: white; border: none; padding: 5px 10px; border-radius: 3px; cursor: pointer;">
                                    🗑️ Delete
                                </button>
                            </div>
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

    // Update store markers
    useEffect(() => {
        if (!mapRef.current) return;

        storeMarkersRef.current.forEach(marker => {
            mapRef.current?.removeLayer(marker);
        });
        storeMarkersRef.current = [];

        stores.forEach(store => {
            if (store.latitude && store.longitude) {
                const storeIcon = L.icon({
                    iconUrl: 'src/images/store128.png',
                    iconSize: [32, 32],
                    iconAnchor: [16, 32],
                    popupAnchor: [0, -32]
                });

                const marker = L.marker([store.latitude, store.longitude], { icon: storeIcon })
                    .addTo(mapRef.current!)
                    .bindPopup(`
                        <div>
                            <b>🏪 ${store.name}</b><br>
                            Store<br>
                            Lat: ${store.latitude.toFixed(4)}<br>
                            Lon: ${store.longitude.toFixed(4)}<br>
                            <div style="margin-top: 10px;">
                                <button onclick="window.showStoreInventory(${store.id})"
                                        style="background: #17a2b8; color: white; border: none; padding: 5px 10px; margin-right: 5px; border-radius: 3px; cursor: pointer;">
                                    📋 Inventory
                                </button>
                                <button onclick="window.deleteStore(${store.id})"
                                        style="background: #dc3545; color: white; border: none; padding: 5px 10px; border-radius: 3px; cursor: pointer;">
                                    🗑️ Delete
                                </button>
                            </div>
                        </div>
                    `);

                storeMarkersRef.current.push(marker);
            }
        });

        (window as any).showStoreInventory = (storeId: number) => {
            if (onShowStoreInventory) onShowStoreInventory(storeId);
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

    // OPTIMIZED delivery markers update
    useEffect(() => {
        if (!mapRef.current) return;

        const currentDeliveryIds = Object.keys(deliveries);
        const existingDeliveryIds = Object.keys(deliveryMarkersRef.current);

        // Remove markers for deliveries that no longer exist
        existingDeliveryIds.forEach(deliveryId => {
            if (!currentDeliveryIds.includes(deliveryId)) {
                const marker = deliveryMarkersRef.current[deliveryId];
                if (marker) {
                    mapRef.current?.removeLayer(marker);
                    delete deliveryMarkersRef.current[deliveryId];
                }
            }
        });

        // Update or create markers for current deliveries
        Object.entries(deliveries).forEach(([deliveryId, delivery]) => {
            if (delivery.currentLatitude && delivery.currentLongitude) {
                const existingMarker = deliveryMarkersRef.current[deliveryId]; if (existingMarker) {
                    // SMOOTH update of existing marker position - NO map refresh                    // Only update if position actually changed
                    if (hasPositionChanged(deliveryId, delivery)) {
                        // Use requestAnimationFrame for smooth updates
                        requestAnimationFrame(() => {
                            existingMarker.setLatLng([delivery.currentLatitude!, delivery.currentLongitude!]);
                        });
                    }

                    // Update popup content
                    existingMarker.setPopupContent(`
                        <div>
                            <b>🚛 Delivery #${deliveryId}</b><br>
                            Status: ${delivery.status}<br>
                            Driver: ${delivery.driverId || 'Not assigned'}<br>
                            Last Update: ${delivery.lastLocationUpdate ?
                            new Date(delivery.lastLocationUpdate).toLocaleTimeString() : 'Never'}<br>
                            ${delivery.status === 'InTransit' ?
                            `<button onclick="window.markAsDelivered(${deliveryId})"
                                        style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 10px; border-radius: 3px; cursor: pointer;">
                                    ✅ Mark as Delivered
                                </button>` : ''
                        }
                        </div>
                    `);
                } else {
                    // Create new marker only if it doesn't exist
                    const truckIcon = L.icon({
                        iconUrl: 'src/images/truck128.png',
                        iconSize: [32, 32],
                        iconAnchor: [16, 32],
                        popupAnchor: [0, -32]
                    });

                    const marker = L.marker([delivery.currentLatitude, delivery.currentLongitude], { icon: truckIcon })
                        .addTo(mapRef.current!)
                        .bindPopup(`
                            <div>
                                <b>🚛 Delivery #${deliveryId}</b><br>
                                Status: ${delivery.status}<br>
                                Driver: ${delivery.driverId || 'Not assigned'}<br>
                                Last Update: ${delivery.lastLocationUpdate ?
                                new Date(delivery.lastLocationUpdate).toLocaleTimeString() : 'Never'}<br>
                                ${delivery.status === 'InTransit' ?
                                `<button onclick="window.markAsDelivered(${deliveryId})"
                                            style="background: #28a745; color: white; border: none; padding: 5px 10px; margin-top: 10px; border-radius: 3px; cursor: pointer;">
                                        ✅ Mark as Delivered
                                    </button>` : ''
                            }
                            </div>
                        `);

                    deliveryMarkersRef.current[deliveryId] = marker;
                }
            }
        });

        (window as any).markAsDelivered = (deliveryId: number) => {
            if (onMarkAsDelivered) onMarkAsDelivered(deliveryId);
        };
    }, [deliveries, onMarkAsDelivered]); return <div ref={mapContainer} style={{ height: '100%', width: '100%' }} />;
});
