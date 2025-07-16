import React, { useState } from 'react';
import { deliveryApi } from '../services/deliveryApi';

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
    const [paymentMethod, setPaymentMethod] = useState<'CreditCard' | 'Cash' | 'BankTransfer'>('CreditCard');
    const [paidAmount, setPaidAmount] = useState(totalAmount);
    const [loading, setLoading] = useState(false);

    React.useEffect(() => {
        setPaidAmount(totalAmount);
    }, [totalAmount]);

    const handleProcessPayment = async () => {
        if (!deliveryId || !paidAmount) {
            alert('Please fill all payment fields');
            return;
        }

        if (paidAmount < totalAmount) {
            if (!confirm(`Paid amount ($${paidAmount}) is less than total amount ($${totalAmount}). Continue?`)) {
                return;
            }
        }

        const paymentData = {
            amount: paidAmount,
            paymentMethod: paymentMethod
        };

        setLoading(true);
        try {
            await deliveryApi.processPayment(deliveryId, paymentData);
            onPaymentProcessed(deliveryId);
        } catch (error) {
            console.error('Error processing payment:', error);
            alert('Error processing payment. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleCancel = async () => {
        if (confirm('Are you sure you want to cancel this delivery? This action cannot be undone.')) {
            if (deliveryId) {
                onCancel(deliveryId);
            }
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
                </div>

                <div style={{
                    backgroundColor: '#f8f9fa',
                    padding: '20px',
                    borderRadius: '8px',
                    margin: '15px 0'
                }}>
                    <p><strong>Delivery ID:</strong> {deliveryId}</p>
                    <p><strong>Total Amount:</strong> ${totalAmount.toFixed(2)}</p>

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

                    <div className="form-group">
                        <label htmlFor="paidAmount">Paid Amount:</label>
                        <input
                            type="number"
                            id="paidAmount"
                            step="0.01"
                            value={paidAmount}
                            onChange={(e) => setPaidAmount(parseFloat(e.target.value) || 0)}
                            style={{ width: '100%', padding: '10px', margin: '8px 0', borderRadius: '5px', border: '1px solid #ddd' }}
                        />
                    </div>

                    <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                        <button
                            className="btn-primary"
                            onClick={handleProcessPayment}
                            disabled={loading}
                            style={{ flex: 1 }}
                        >
                            {loading ? 'Processing...' : 'Process Payment'}
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
