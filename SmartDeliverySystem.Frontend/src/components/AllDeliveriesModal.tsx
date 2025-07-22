import React, { useState, useEffect } from 'react';
import { DeliveryData } from '../types/delivery';
import { deliveryApi } from '../services/deliveryApi';
import { useToast } from '../contexts/ToastContext';
import { useConfirmation } from '../contexts/ConfirmationContext';

interface AllDeliveriesModalProps {
    isOpen: boolean;
    onClose: () => void;
    onShowProducts: (deliveryId: number) => void;
}

interface DeliveryHistoryItem {
    id: number;
    deliveryId: number;
    vendor: string;
    store: string;
    status: string;
    totalAmount: number;
    createdAt: string;
    deliveredAt?: string;
}

export const AllDeliveriesModal: React.FC<AllDeliveriesModalProps> = ({
    isOpen,
    onClose,
    onShowProducts
}) => {
    const { showSuccess, showError } = useToast();
    const { showConfirmation } = useConfirmation();
    const [deliveries, setDeliveries] = useState<DeliveryHistoryItem[]>([]);
    const [loading, setLoading] = useState(false);
    const [showGPSHistory, setShowGPSHistory] = useState(false);
    const [selectedDeliveryId, setSelectedDeliveryId] = useState<number | null>(null);
    const [gpsHistory, setGpsHistory] = useState<any[]>([]);

    useEffect(() => {
        if (isOpen) {
            loadAllDeliveries();
        }
    }, [isOpen]);

    const getStatusText = (status: number | string) => {
        if (typeof status === 'string') return status;

        // Convert numeric status to text according to enum
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

    const loadAllDeliveries = async () => {
        setLoading(true);
        try {
            // Load all deliveries (not just active ones)
            const allDeliveries = await deliveryApi.getAllDeliveries();

            // Format data for display
            const formattedDeliveries = allDeliveries.map((delivery: any) => ({
                id: delivery.id || delivery.deliveryId,
                deliveryId: delivery.id || delivery.deliveryId,
                vendor: delivery.vendor?.name || delivery.vendorName || `Vendor #${delivery.vendorId}`,
                store: delivery.store?.name || delivery.storeName || `Store #${delivery.storeId}`,
                status: getStatusText(delivery.status),
                totalAmount: delivery.totalAmount || 0,
                createdAt: new Date(delivery.createdAt).toLocaleString(),
                deliveredAt: delivery.deliveredAt ? new Date(delivery.deliveredAt).toLocaleString() : undefined
            }));

            setDeliveries(formattedDeliveries);
        } catch (error) {
            console.error('Error loading all deliveries:', error);
            showError('Error loading deliveries. Please try again.');
            setDeliveries([]);
        } finally {
            setLoading(false);
        }
    };

    const handleShowGPSHistory = async (deliveryId: number) => {
        setLoading(true);
        try {
            const history = await deliveryApi.getDeliveryLocationHistory(deliveryId);
            setGpsHistory(history);
            setSelectedDeliveryId(deliveryId);
            setShowGPSHistory(true);
        } catch (error) {
            console.error('Error loading GPS history:', error);
            showError('Error loading GPS history. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteDelivery = async (deliveryId: number) => {
        const confirmed = await showConfirmation({
            title: 'Delete Delivery',
            message: 'Are you sure you want to delete this delivery? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel',
            confirmColor: '#dc3545'
        });

        if (!confirmed) return;

        setLoading(true);
        try {
            await deliveryApi.deleteDelivery(deliveryId);
            showSuccess('Delivery deleted successfully');
            // Reload deliveries after deletion
            await loadAllDeliveries();
        } catch (error) {
            console.error('Error deleting delivery:', error);
            showError('Error deleting delivery. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const getStatusColor = (status: string) => {
        switch (status) {
            case 'PendingPayment': return '#ffc107';
            case 'Paid': return '#17a2b8';
            case 'Assigned': return '#fd7e14';
            case 'InTransit': return '#007bff';
            case 'Delivered': return '#28a745';
            case 'Cancelled': return '#dc3545';
            default: return '#6c757d';
        }
    };

    if (!isOpen) return null;

    return (
        <div className="modal">
            <div className="modal-content" style={{ width: '90%', maxWidth: '1000px' }}>
                <div className="modal-header">
                    <h3>📋 All Deliveries</h3>
                    <button className="close-button" onClick={onClose}>×</button>
                </div>

                {loading ? (
                    <div style={{ textAlign: 'center', padding: '20px' }}>
                        Loading deliveries...
                    </div>
                ) : (
                    <div style={{ maxHeight: '500px', overflowY: 'auto' }}>
                        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                            <thead>
                                <tr style={{ backgroundColor: '#f8f9fa' }}>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>ID</th>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Vendor</th>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Store</th>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Status</th>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Total</th>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Created</th>
                                    <th style={{ padding: '10px', border: '1px solid #ddd', textAlign: 'left' }}>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {deliveries.map(delivery => (
                                    <tr key={delivery.id}>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>{delivery.deliveryId}</td>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>{delivery.vendor}</td>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>{delivery.store}</td>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>
                                            <span style={{
                                                backgroundColor: getStatusColor(delivery.status),
                                                color: 'white',
                                                padding: '2px 8px',
                                                borderRadius: '12px',
                                                fontSize: '12px'
                                            }}>
                                                {delivery.status}
                                            </span>
                                        </td>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>${delivery.totalAmount.toFixed(2)}</td>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>{delivery.createdAt}</td>
                                        <td style={{ padding: '10px', border: '1px solid #ddd' }}>
                                            <div style={{ display: 'flex', gap: '5px' }}>
                                                <button className='btn-multiple'
                                                    onClick={() => handleShowGPSHistory(delivery.deliveryId)}
                                                    style={{
                                                        padding: '4px 8px',
                                                        backgroundColor: '#17a2b8',
                                                        color: 'white',
                                                        border: 'none',
                                                        borderRadius: '3px',
                                                        fontSize: '11px',
                                                        cursor: 'pointer'
                                                    }}
                                                >
                                                    📍 GPS History
                                                </button>
                                                <button className='save-button'
                                                    onClick={() => onShowProducts(delivery.deliveryId)}
                                                    style={{
                                                        padding: '4px 8px',
                                                        backgroundColor: '#28a745',
                                                        color: 'white',
                                                        border: 'none',
                                                        borderRadius: '3px',
                                                        fontSize: '11px',
                                                        cursor: 'pointer'
                                                    }}
                                                >
                                                    📦 Products
                                                </button>
                                                <button className='control-buttons--cancel'
                                                    onClick={() => handleDeleteDelivery(delivery.deliveryId)}
                                                    style={{
                                                        padding: '4px 8px',
                                                        backgroundColor: '#dc3545',
                                                        color: 'white',
                                                        border: 'none',
                                                        borderRadius: '3px',
                                                        fontSize: '11px',
                                                        cursor: 'pointer'
                                                    }}
                                                    disabled={loading}
                                                >
                                                    ❌ Delete
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}

                {/* GPS History Modal */}
                {showGPSHistory && (
                    <div className="modal" style={{ backgroundColor: 'rgba(0,0,0,0.7)' }}>
                        <div className="modal-content" style={{ width: '80%', maxWidth: '800px' }}>
                            <div className="modal-header">
                                <h3>📍 GPS History - Delivery #{selectedDeliveryId}</h3>
                                <button className="close-button" onClick={() => setShowGPSHistory(false)}>×</button>
                            </div>
                            <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                                {gpsHistory.length === 0 ? (
                                    <p>No GPS history available for this delivery.</p>
                                ) : (
                                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                                        <thead>
                                            <tr style={{ backgroundColor: '#f8f9fa' }}>
                                                <th style={{ padding: '8px', border: '1px solid #ddd' }}>Time</th>
                                                <th style={{ padding: '8px', border: '1px solid #ddd' }}>Latitude</th>
                                                <th style={{ padding: '8px', border: '1px solid #ddd' }}>Longitude</th>
                                                <th style={{ padding: '8px', border: '1px solid #ddd' }}>Notes</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {gpsHistory.map((record, index) => (
                                                <tr key={index}>
                                                    <td style={{ padding: '8px', border: '1px solid #ddd' }}>
                                                        {new Date(record.timestamp).toLocaleString()}
                                                    </td>
                                                    <td style={{ padding: '8px', border: '1px solid #ddd' }}>
                                                        {record.latitude.toFixed(6)}
                                                    </td>
                                                    <td style={{ padding: '8px', border: '1px solid #ddd' }}>
                                                        {record.longitude.toFixed(6)}
                                                    </td>
                                                    <td style={{ padding: '8px', border: '1px solid #ddd' }}>
                                                        {record.notes || '-'}
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                )}
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};
