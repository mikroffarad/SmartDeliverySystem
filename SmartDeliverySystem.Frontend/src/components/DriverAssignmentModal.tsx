import React, { useState } from 'react';
import { deliveryApi } from '../services/deliveryApi';
import { useToast } from '../contexts/ToastContext';
import { useConfirmation } from '../contexts/ConfirmationContext';

interface DriverAssignmentModalProps {
    isOpen: boolean;
    deliveryId: number | null;
    onClose: () => void;
    onDriverAssigned: () => void;
    onCancel: (deliveryId: number) => void;
}

export const DriverAssignmentModal: React.FC<DriverAssignmentModalProps> = ({
    isOpen,
    deliveryId,
    onClose,
    onDriverAssigned,
    onCancel
}) => {
    const { showSuccess, showError } = useToast();
    const { showConfirmation } = useConfirmation();
    const [driverId, setDriverId] = useState('');
    const [gpsTrackerId, setGpsTrackerId] = useState('');
    const [loading, setLoading] = useState(false);

    // Auto-fill driver and GPS tracker IDs when deliveryId changes
    React.useEffect(() => {
        if (deliveryId) {
            setDriverId(`DRIVER_${deliveryId}`);
            setGpsTrackerId(`GPS_${deliveryId}`);
        }
    }, [deliveryId]); const handleAssignDriver = async () => {
        if (!deliveryId || !driverId || !gpsTrackerId) {
            showError('Please fill all driver fields');
            return;
        }

        const driverData = {
            driverId: driverId,
            gpsTrackerId: gpsTrackerId
        };

        setLoading(true); try {
            await deliveryApi.assignDriver(deliveryId, driverData);
            showSuccess(`🎉 Delivery activated successfully!\n\nDelivery ID: ${deliveryId}\nDriver: ${driverId}\nGPS Tracker: ${gpsTrackerId}\n\nThe delivery is now active and tracking will begin.`);
            onDriverAssigned();
            handleClose();
        } catch (error) {
            console.error('Error assigning driver:', error);
            showError('Error assigning driver. Please try again.');
        } finally {
            setLoading(false);
        }
    }; const handleCancel = async () => {
        const confirmed = await showConfirmation({
            title: 'Cancel Delivery',
            message: 'Are you sure you want to cancel this delivery? This action cannot be undone.',
            confirmText: 'Cancel Delivery',
            cancelText: 'Keep Delivery',
            confirmColor: '#dc3545'
        });

        if (confirmed && deliveryId) {
            onCancel(deliveryId);
        }
    };

    const handleClose = () => {
        setDriverId('');
        setGpsTrackerId('');
        onClose();
    };

    if (!isOpen || !deliveryId) return null;

    return (
        <div className="modal">
            <div className="modal-content">
                <div className="modal-header">
                    <h3>👨‍💼 Assign Driver</h3>
                    <button className="close-button" onClick={handleClose}>×</button>
                </div>

                {/* Step Indicator */}
                <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '20px' }}>
                    <div style={{
                        padding: '8px 16px',
                        margin: '0 5px',
                        borderRadius: '20px',
                        border: '5px solid #28a745',
                        fontSize: '14px',
                        fontWeight: 'bold',
                        background: '#28a745',
                        color: 'white'
                    }}>
                        💰 Payment
                    </div>
                    <div style={{
                        padding: '8px 16px',
                        margin: '0 5px',
                        borderRadius: '20px',
                        border: '5px solid orange',
                        fontSize: '14px',
                        fontWeight: 'bold',
                        background: '#007bff',
                        color: 'white'
                    }}>
                        👨‍💼 Driver
                    </div>
                    <div style={{
                        padding: '8px 16px',
                        margin: '0 5px',
                        borderRadius: '20px',
                        border: '5px solid #6c757d',
                        fontSize: '14px',
                        fontWeight: 'bold',
                        background: '#6c757d',
                        color: 'white'
                    }}>
                        🚛 Active
                    </div>
                </div>                <div style={{
                    backgroundColor: '#f8f9fa',
                    padding: '20px',
                    borderRadius: '8px',
                    margin: '15px 0'
                }}>
                    <p><strong>Delivery ID:</strong> {deliveryId}</p>
                    <p><strong>Status:</strong> Paid ✅</p>

                    <div style={{
                        backgroundColor: '#e9f7ef',
                        border: '1px solid #c3e6cb',
                        borderRadius: '5px',
                        padding: '10px',
                        marginBottom: '15px',
                        marginTop: '15px',
                        fontSize: '14px'
                    }}>
                        <strong>ℹ️ Note:</strong> Driver and GPS Tracker IDs are automatically generated based on the delivery ID.
                    </div><div className="form-group">
                        <label htmlFor="driverId">Driver ID:</label>
                        <input
                            type="text"
                            id="driverId"
                            value={driverId}
                            readOnly
                            placeholder="Auto-generated based on delivery ID"
                            style={{
                                width: '100%',
                                padding: '10px',
                                margin: '8px 0',
                                borderRadius: '5px',
                                border: '1px solid #ddd',
                                backgroundColor: '#f8f9fa',
                                color: '#495057'
                            }}
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="gpsTrackerId">GPS Tracker ID:</label>
                        <input
                            type="text"
                            id="gpsTrackerId"
                            value={gpsTrackerId}
                            readOnly
                            placeholder="Auto-generated based on delivery ID"
                            style={{
                                width: '100%',
                                padding: '10px',
                                margin: '8px 0',
                                borderRadius: '5px',
                                border: '1px solid #ddd',
                                backgroundColor: '#f8f9fa',
                                color: '#495057'
                            }}
                        />
                    </div>

                    <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                        <button
                            className="btn-primary"
                            onClick={handleAssignDriver}
                            disabled={loading}
                            style={{ flex: 1 }}
                        >
                            {loading ? 'Assigning...' : 'Assign Driver & Activate'}
                        </button>
                        <button
                            onClick={handleCancel}
                            style={{
                                background: '#dc3545',
                                color: 'white',
                                border: 'none',
                                padding: '10px 20px',
                                borderRadius: '5px',
                                cursor: 'pointer'
                            }}
                        >
                            ❌ Cancel Delivery
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};
