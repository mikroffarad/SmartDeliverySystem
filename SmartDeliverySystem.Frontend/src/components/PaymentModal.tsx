import React, { useState } from 'react';
import { deliveryApi } from '../services/deliveryApi';
import { useToast } from '../contexts/ToastContext';
import { useConfirmation } from '../contexts/ConfirmationContext';

interface PaymentModalProps {
    isOpen: boolean;
    deliveryId: number | null;
    totalAmount: number;
    onClose: () => void;
    onPaymentProcessed: (deliveryId: number) => void;
    onCancel: (deliveryId: number) => void;
}

export const PaymentModal: React.FC<PaymentModalProps> = ({
    isOpen,
    deliveryId,
    totalAmount,
    onClose,
    onPaymentProcessed,
    onCancel
}) => {
    const { showError } = useToast();
    const { showConfirmation } = useConfirmation();
    const [paymentMethod, setPaymentMethod] = useState<'CreditCard' | 'Cash' | 'BankTransfer'>('CreditCard');
    const [loading, setLoading] = useState(false);

    const handleProcessPayment = async () => {
        if (!deliveryId) {
            showError('Please select a payment method');
            return;
        }

        const paymentData = {
            amount: totalAmount, // Always use the exact total amount
            paymentMethod: paymentMethod
        }; setLoading(true);
        try {
            await deliveryApi.processPayment(deliveryId, paymentData);
            onPaymentProcessed(deliveryId);
        } catch (error: any) {
            console.error('Error processing payment:', error);
            // Show the specific error message from the server
            const errorMessage = error.message || 'Error processing payment. Please try again.';
            showError(errorMessage);
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

    if (!isOpen || !deliveryId) return null;

    return (
        <div className="modal">
            <div className="modal-content">
                <div className="modal-header">
                    <h3>💰 Payment Required</h3>
                    <button className="close-button" onClick={onClose}>×</button>
                </div>

                {/* Step Indicator */}
                <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '20px' }}>
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
                        💰 Payment
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
                    <p><strong>Total Amount:</strong> ${totalAmount.toFixed(2)}</p>
                    <p style={{ color: '#007bff', fontWeight: 'bold' }}>⚠️ You must pay exactly ${totalAmount.toFixed(2)}</p>

                    <div className="form-group">
                        <label htmlFor="paymentMethod">Payment Method:</label>
                        <select
                            id="paymentMethod"
                            value={paymentMethod}
                            onChange={(e) => setPaymentMethod(e.target.value as 'CreditCard' | 'Cash' | 'BankTransfer')}
                            style={{ width: '100%', padding: '10px', margin: '8px 0', borderRadius: '5px', border: '1px solid #ddd' }}
                        >
                            <option value="CreditCard">Credit Card</option>
                            <option value="Cash">Cash</option>
                            <option value="BankTransfer">Bank Transfer</option>
                        </select>
                    </div>

                    <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                        <button
                            className="btn-primary"
                            onClick={handleProcessPayment}
                            disabled={loading}
                            style={{ flex: 1 }}
                        >
                            {loading ? 'Processing...' : `Pay $${totalAmount.toFixed(2)}`}
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
