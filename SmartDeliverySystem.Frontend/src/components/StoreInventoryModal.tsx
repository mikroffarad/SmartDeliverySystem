import React, { useState, useEffect } from 'react';
import { deliveryApi } from '../services/deliveryApi';
import { InventoryItem } from '../types/delivery';

interface StoreInventoryModalProps {
    isOpen: boolean;
    storeId: number | null;
    storeName: string;
    onClose: () => void;
}

export const StoreInventoryModal: React.FC<StoreInventoryModalProps> = ({
    isOpen,
    storeId,
    storeName,
    onClose
}) => {
    const [inventory, setInventory] = useState<InventoryItem[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (isOpen && storeId) {
            loadInventory();
        }
    }, [isOpen, storeId]);

    const loadInventory = async () => {
        if (!storeId) return;

        setLoading(true);
        setError(null);

        try {
            const inventoryData = await deliveryApi.getStoreInventory(storeId);
            setInventory(inventoryData);
        } catch (err) {
            console.error('Error loading store inventory:', err);
            setError('Failed to load inventory. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleClose = () => {
        setInventory([]);
        setError(null);
        onClose();
    };

    if (!isOpen) return null;

    return (
        <div style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            zIndex: 1000
        }}>
            <div style={{
                backgroundColor: 'white',
                borderRadius: '8px',
                padding: '20px',
                width: '90%',
                maxWidth: '800px',
                maxHeight: '80vh',
                overflow: 'auto',
                boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
            }}>
                <div style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    marginBottom: '20px',
                    borderBottom: '1px solid #eee',
                    paddingBottom: '10px'
                }}>
                    <h2 style={{ margin: 0, color: '#333' }}>
                        📋 Inventory - {storeName}
                    </h2>
                    <button
                        onClick={handleClose}
                        style={{
                            background: '#6c757d',
                            color: 'white',
                            border: 'none',
                            borderRadius: '4px',
                            padding: '8px 12px',
                            cursor: 'pointer'
                        }}
                    >
                        ✖️ Close
                    </button>
                </div>

                {loading && (
                    <div style={{ textAlign: 'center', padding: '40px' }}>
                        <div>🔄 Loading inventory...</div>
                    </div>
                )}

                {error && (
                    <div style={{
                        backgroundColor: '#f8d7da',
                        color: '#721c24',
                        padding: '10px',
                        borderRadius: '4px',
                        marginBottom: '20px'
                    }}>
                        ❌ {error}
                    </div>
                )}

                {!loading && !error && (
                    <>
                        <div style={{
                            backgroundColor: '#d4edda',
                            color: '#155724',
                            padding: '10px',
                            borderRadius: '4px',
                            marginBottom: '20px'
                        }}>
                            📦 Total products in inventory: <strong>{inventory.length}</strong>
                            {inventory.length > 0 && (
                                <> | Total quantity: <strong>{inventory.reduce((sum, item) => sum + item.quantity, 0)}</strong></>
                            )}
                        </div>

                        {inventory.length === 0 ? (
                            <div style={{
                                textAlign: 'center',
                                padding: '40px',
                                color: '#6c757d'
                            }}>
                                <div style={{ fontSize: '48px', marginBottom: '20px' }}>📦</div>
                                <h3>No Products in Inventory</h3>
                                <p>
                                    This store doesn't have any products yet.<br />
                                    Products will appear here after deliveries are completed.
                                </p>
                                <div style={{
                                    backgroundColor: '#e9ecef',
                                    padding: '15px',
                                    borderRadius: '4px',
                                    marginTop: '20px'
                                }}>
                                    <strong>💡 How to add products:</strong><br />
                                    1. Create a delivery from a vendor to this store<br />
                                    2. Complete the delivery process (payment + driver assignment)<br />
                                    3. When the delivery arrives, products will be added to inventory
                                </div>
                            </div>
                        ) : (
                            <div style={{ overflow: 'auto' }}>
                                <table style={{
                                    width: '100%',
                                    borderCollapse: 'collapse',
                                    marginTop: '10px'
                                }}>
                                    <thead>
                                        <tr style={{ backgroundColor: '#f8f9fa' }}>
                                            <th style={tableHeaderStyle}>Product Name</th>
                                            <th style={tableHeaderStyle}>Category</th>
                                            <th style={tableHeaderStyle}>Quantity</th>
                                            <th style={tableHeaderStyle}>Price per Unit</th>
                                            <th style={tableHeaderStyle}>Total Value</th>
                                            <th style={tableHeaderStyle}>Last Updated</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {inventory.map((item, index) => (
                                            <tr key={item.id} style={{
                                                backgroundColor: index % 2 === 0 ? 'white' : '#f8f9fa'
                                            }}>
                                                <td style={tableCellStyle}>
                                                    <strong>{item.productName}</strong>
                                                </td>
                                                <td style={tableCellStyle}>
                                                    <span style={{
                                                        padding: '4px 8px',
                                                        backgroundColor: '#e9ecef',
                                                        borderRadius: '12px',
                                                        fontSize: '12px'
                                                    }}>
                                                        {item.category}
                                                    </span>
                                                </td>
                                                <td style={{ ...tableCellStyle, textAlign: 'center', fontWeight: 'bold' }}>
                                                    {item.quantity}
                                                </td>
                                                <td style={{ ...tableCellStyle, textAlign: 'right' }}>
                                                    ${item.price.toFixed(2)}
                                                </td>
                                                <td style={{ ...tableCellStyle, textAlign: 'right', fontWeight: 'bold' }}>
                                                    ${(item.quantity * item.price).toFixed(2)}
                                                </td>                                                <td style={{ ...tableCellStyle, fontSize: '12px', color: '#6c757d' }}>
                                                    {/* Since lastUpdated might not exist, we'll show a placeholder */}
                                                    Recent
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>

                                <div style={{
                                    marginTop: '20px',
                                    padding: '15px',
                                    backgroundColor: '#f8f9fa',
                                    borderRadius: '4px',
                                    display: 'flex',
                                    justifyContent: 'space-between'
                                }}>
                                    <div>
                                        <strong>💰 Total Inventory Value:</strong>
                                    </div>
                                    <div style={{ fontSize: '18px', fontWeight: 'bold', color: '#28a745' }}>
                                        ${inventory.reduce((sum, item) => sum + (item.quantity * item.price), 0).toFixed(2)}
                                    </div>
                                </div>
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
};

const tableHeaderStyle: React.CSSProperties = {
    padding: '12px',
    textAlign: 'left',
    borderBottom: '2px solid #dee2e6',
    fontWeight: 'bold',
    color: '#495057'
};

const tableCellStyle: React.CSSProperties = {
    padding: '12px',
    borderBottom: '1px solid #dee2e6'
};
