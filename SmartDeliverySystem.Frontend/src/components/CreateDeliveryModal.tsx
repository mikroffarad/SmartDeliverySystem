import React, { useState, useEffect } from 'react';
import { VendorData, StoreData, ProductData } from '../types/delivery';
import { deliveryApi } from '../services/deliveryApi';

interface CreateDeliveryModalProps {
    isOpen: boolean;
    vendorId: number | null;
    vendorName: string;
    onClose: () => void;
    onDeliveryCreated: (deliveryId: number, totalAmount: number) => void;
}

interface ProductSelection {
    productId: number;
    name: string;
    price: number;
    category: string;
    quantity: number;
}

export const CreateDeliveryModal: React.FC<CreateDeliveryModalProps> = ({
    isOpen,
    vendorId,
    vendorName,
    onClose,
    onDeliveryCreated
}) => {
    const [products, setProducts] = useState<ProductData[]>([]);
    const [stores, setStores] = useState<StoreData[]>([]);
    const [selectedStoreId, setSelectedStoreId] = useState<number | null>(null);
    const [productSelections, setProductSelections] = useState<Record<number, number>>({});
    const [loading, setLoading] = useState(false);
    const [autoSelectLoading, setAutoSelectLoading] = useState(false);

    useEffect(() => {
        if (isOpen && vendorId) {
            loadData();
        }
    }, [isOpen, vendorId]);

    const loadData = async () => {
        setLoading(true);
        try {
            const [vendorProducts, allStores] = await Promise.all([
                deliveryApi.getVendorProducts(vendorId!),
                deliveryApi.getAllStores()
            ]);
            setProducts(vendorProducts);
            setStores(allStores);
        } catch (error) {
            console.error('Error loading data:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleQuantityChange = (productId: number, quantity: number) => {
        setProductSelections(prev => ({
            ...prev,
            [productId]: Math.max(0, quantity)
        }));
    };

    const calculateTotal = (): number => {
        return products.reduce((total, product) => {
            const quantity = productSelections[product.id || 0] || 0;
            return total + (quantity * product.price);
        }, 0);
    };

    const getSelectedProducts = () => {
        return Object.entries(productSelections)
            .filter(([_, quantity]) => quantity > 0)
            .map(([productId, quantity]) => ({
                productId: parseInt(productId),
                quantity
            }));
    };

    const handleAutoSelectStore = async () => {
        const selectedProducts = getSelectedProducts();
        if (selectedProducts.length === 0) {
            alert('Please select at least one product with quantity > 0 before auto-selecting store');
            return;
        }

        setAutoSelectLoading(true);
        try {
            const result = await deliveryApi.findBestStore(vendorId!, selectedProducts);
            setSelectedStoreId(result.storeId);
            alert(`🎯 Best store selected: ${result.storeName}\n\nDistance: ${result.distance?.toFixed(2)} km\nReason: Optimal combination of distance and inventory balance`);
        } catch (error) {
            console.error('Error auto-selecting store:', error);
            alert('Error finding best store. Please try again.');
        } finally {
            setAutoSelectLoading(false);
        }
    };

    const handleCreateDelivery = async () => {
        const selectedProducts = getSelectedProducts();

        if (selectedProducts.length === 0) {
            alert('Please select at least one product with quantity > 0');
            return;
        }

        if (!selectedStoreId) {
            alert('Please select a destination store or use Auto-select');
            return;
        }

        const deliveryRequest = {
            vendorId: vendorId!,
            storeId: selectedStoreId,
            products: selectedProducts
        };

        setLoading(true);
        try {
            const result = await deliveryApi.createDeliveryRequest(deliveryRequest);
            onDeliveryCreated(result.deliveryId, result.totalAmount);
            handleClose();
        } catch (error) {
            console.error('Error creating delivery:', error);
            alert('Error creating delivery. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleClose = () => {
        setSelectedStoreId(null);
        setProductSelections({});
        onClose();
    };

    if (!isOpen) return null;

    return (
        <div className="modal">
            <div className="modal-content" style={{ maxWidth: '800px', width: '90%' }}>
                <div className="modal-header">
                    <h3>🚛 Create Delivery - {vendorName}</h3>
                    <button className="close-button" onClick={handleClose}>×</button>
                </div>

                {loading ? (
                    <div style={{ textAlign: 'center', padding: '20px' }}>
                        Loading...
                    </div>
                ) : (
                    <div>
                        {/* Store Selection */}
                        <div className="form-group">
                            <label>Select Destination Store:</label>
                            <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                                <select
                                    value={selectedStoreId || ''}
                                    onChange={(e) => setSelectedStoreId(e.target.value ? parseInt(e.target.value) : null)}
                                    style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #ddd' }}
                                >
                                    <option value="">Choose a store...</option>
                                    {stores.map(store => (
                                        <option key={store.id} value={store.id}>
                                            {store.name} (Lat: {store.latitude?.toFixed(4)}, Lon: {store.longitude?.toFixed(4)})
                                        </option>
                                    ))}
                                </select>
                                <button
                                    onClick={handleAutoSelectStore}
                                    disabled={autoSelectLoading}
                                    style={{
                                        background: '#28a745',
                                        color: 'white',
                                        border: 'none',
                                        padding: '8px 12px',
                                        borderRadius: '4px',
                                        cursor: autoSelectLoading ? 'not-allowed' : 'pointer',
                                        whiteSpace: 'nowrap'
                                    }}
                                >
                                    {autoSelectLoading ? '🔄 Finding...' : '🎯 Auto-select best store'}
                                </button>
                            </div>
                        </div>

                        {/* Product Selection */}
                        <div className="form-group">
                            <label>Select Products and Quantities:</label>
                            <div style={{
                                border: '1px solid #ddd',
                                borderRadius: '5px',
                                padding: '10px',
                                maxHeight: '300px',
                                overflowY: 'auto',
                                backgroundColor: 'white'
                            }}>
                                {products.length === 0 ? (
                                    <p>No products available for this vendor</p>
                                ) : (
                                    products.map(product => (
                                        <div key={product.id} style={{
                                            display: 'flex',
                                            justifyContent: 'space-between',
                                            alignItems: 'center',
                                            padding: '5px 0',
                                            borderBottom: '1px solid #eee'
                                        }}>
                                            <div>
                                                <strong>{product.name}</strong><br />
                                                <small>{product.category} - ${product.price.toFixed(2)}/unit</small>
                                            </div>
                                            <div style={{ display: 'flex', alignItems: 'center', gap: '5px' }}>
                                                <input
                                                    type="number"
                                                    min="0"
                                                    value={productSelections[product.id || 0] || 0}
                                                    onChange={(e) => handleQuantityChange(product.id || 0, parseInt(e.target.value) || 0)}
                                                    style={{
                                                        width: '80px',
                                                        padding: '5px',
                                                        border: '1px solid #ddd',
                                                        borderRadius: '3px',
                                                        textAlign: 'center'
                                                    }}
                                                />
                                                <label>qty</label>
                                            </div>
                                        </div>
                                    ))
                                )}
                            </div>
                        </div>

                        {/* Total Amount */}
                        <div style={{ margin: '15px 0', fontSize: '18px', fontWeight: 'bold' }}>
                            Total Amount: ${calculateTotal().toFixed(2)}
                        </div>

                        {/* Action Buttons */}
                        <div className="form-actions">
                            <button className="btn-secondary" onClick={handleClose}>
                                Cancel
                            </button>
                            <button
                                className="btn-primary"
                                onClick={handleCreateDelivery}
                                disabled={loading}
                            >
                                {loading ? 'Creating...' : 'Create Delivery Request'}
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};
