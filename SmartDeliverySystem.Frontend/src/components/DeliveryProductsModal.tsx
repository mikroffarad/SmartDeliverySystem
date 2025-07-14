import React, { useState, useEffect } from 'react';
import { deliveryApi } from '../services/deliveryApi';

interface DeliveryProductsModalProps {
    isOpen: boolean;
    deliveryId: number | null;
    onClose: () => void;
}

interface DeliveryProduct {
    id: number;
    name: string;
    category: string;
    weight: number;
    price: number;
    quantity: number;
}

export const DeliveryProductsModal: React.FC<DeliveryProductsModalProps> = ({
    isOpen,
    deliveryId,
    onClose
}) => {
    const [products, setProducts] = useState<DeliveryProduct[]>([]);
    const [loading, setLoading] = useState(false);
    const [deliveryInfo, setDeliveryInfo] = useState<any>(null);

    useEffect(() => {
        if (isOpen && deliveryId) {
            loadDeliveryProducts();
        }
    }, [isOpen, deliveryId]);

    const loadDeliveryProducts = async () => {
        if (!deliveryId) return;

        setLoading(true);
        try {
            // Завантажуємо інформацію про доставку
            const delivery = await deliveryApi.getDeliveryById(deliveryId);
            setDeliveryInfo(delivery);

            // Завантажуємо продукти доставки
            const deliveryProducts = await deliveryApi.getDeliveryProducts(deliveryId);
            setProducts(deliveryProducts);
        } catch (error) {
            console.error('Error loading delivery products:', error);
            setProducts([]);
        } finally {
            setLoading(false);
        }
    };

    if (!isOpen || !deliveryId) return null;

    return (
        <div className="modal">
            <div className="modal-content" style={{ width: '80%', maxWidth: '800px' }}>
                <div className="modal-header">
                    <h3>📦 Products - Delivery #{deliveryId}</h3>
                    <button className="close-button" onClick={onClose}>×</button>
                </div>

                {loading ? (
                    <div style={{ textAlign: 'center', padding: '20px' }}>
                        Loading products...
                    </div>
                ) : (
                    <div>
                        {deliveryInfo && (
                            <div style={{
                                backgroundColor: '#f8f9fa',
                                padding: '15px',
                                marginBottom: '20px',
                                borderRadius: '5px'
                            }}>
                                <h4>Delivery Information</h4>
                                <p><strong>Vendor:</strong> {deliveryInfo.vendor?.name || `Vendor #${deliveryInfo.vendorId}`}</p>
                                <p><strong>Store:</strong> {deliveryInfo.store?.name || `Store #${deliveryInfo.storeId}`}</p>
                                <p><strong>Status:</strong> {deliveryInfo.status}</p>
                                <p><strong>Total Amount:</strong> ${deliveryInfo.totalAmount?.toFixed(2) || '0.00'}</p>
                            </div>
                        )}

                        {products.length === 0 ? (
                            <p>No products found for this delivery.</p>
                        ) : (
                            <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                                    <thead>
                                        <tr style={{ backgroundColor: '#f8f9fa' }}>
                                            <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Product Name</th>
                                            <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Category</th>
                                            <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Weight (kg)</th>
                                            <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Price</th>
                                            <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Quantity</th>
                                            <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Total</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {products.map(product => (
                                            <tr key={product.id}>
                                                <td style={{ padding: '10px', border: '1px solid #ddd' }}>{product.name}</td>
                                                <td style={{ padding: '10px', border: '1px solid #ddd' }}>
                                                    <span style={{
                                                        backgroundColor: '#e9ecef',
                                                        padding: '2px 8px',
                                                        borderRadius: '12px',
                                                        fontSize: '12px'
                                                    }}>
                                                        {product.category}
                                                    </span>
                                                </td>
                                                <td style={{ padding: '10px', border: '1px solid #ddd' }}>{product.weight}</td>
                                                <td style={{ padding: '10px', border: '1px solid #ddd' }}>${product.price.toFixed(2)}</td>
                                                <td style={{ padding: '10px', border: '1px solid #ddd' }}>{product.quantity}</td>
                                                <td style={{ padding: '10px', border: '1px solid #ddd' }}>
                                                    <strong>${(product.price * product.quantity).toFixed(2)}</strong>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}

                        <div style={{ textAlign: 'right', marginTop: '20px' }}>
                            <button
                                onClick={onClose}
                                style={{
                                    padding: '10px 20px',
                                    backgroundColor: '#6c757d',
                                    color: 'white',
                                    border: 'none',
                                    borderRadius: '5px'
                                }}
                            >
                                Close
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};
