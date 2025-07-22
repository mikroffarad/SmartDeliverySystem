import React, { useState, useEffect } from 'react';
import { ProductData } from '../types/delivery';
import { deliveryApi } from '../services/deliveryApi';
import { useToast } from '../contexts/ToastContext';
import { useConfirmation } from '../contexts/ConfirmationContext';

interface VendorProductsModalProps {
    isOpen: boolean;
    vendorId: number | null;
    onClose: () => void;
}

export const VendorProductsModal: React.FC<VendorProductsModalProps> = ({
    isOpen,
    vendorId,
    onClose
}) => {
    const { showSuccess, showError } = useToast();
    const { showConfirmation } = useConfirmation();
    const [products, setProducts] = useState<ProductData[]>([]);
    const [vendorName, setVendorName] = useState<string>('');
    const [loading, setLoading] = useState(false);
    const [showAddProduct, setShowAddProduct] = useState(false);
    const [editingProduct, setEditingProduct] = useState<ProductData | null>(null);
    const [bulkMode, setBulkMode] = useState(false);
    const [csvData, setCsvData] = useState('');
    const [newProduct, setNewProduct] = useState({
        name: '',
        weight: 0,
        category: '',
        price: 0
    });

    // Predefined categories for products
    const predefinedCategories = [
        'Bakery',
        'Dairy',
        'Meat',
        'Seafood',
        'Fruits',
        'Vegetables',
        'Frozen Foods',
        'Beverages',
        'Snacks',
        'Health',
        'Beauty',
        'Household Items',
        'Other'
    ];

    useEffect(() => {
        if (isOpen && vendorId) {
            loadVendorProducts();
        }
    }, [isOpen, vendorId]);

    const loadVendorProducts = async () => {
        if (!vendorId) return;

        setLoading(true);
        try {
            const vendors = await deliveryApi.getAllVendors();
            const vendor = vendors.find(v => v.id === vendorId);
            setVendorName(vendor?.name || `Vendor #${vendorId}`);

            const vendorProducts = await deliveryApi.getVendorProducts(vendorId);
            setProducts(vendorProducts);
        } catch (error) {
            console.error('Error loading vendor products:', error);
            setProducts([]);
        } finally {
            setLoading(false);
        }
    }; const handleAddProduct = async () => {
        if (!vendorId || !newProduct.name || !newProduct.category || newProduct.price <= 0) {
            showError('Please fill in all required fields including category');
            return;
        }

        try {
            await deliveryApi.addProductToVendor(vendorId, newProduct);
            showSuccess('Product added successfully!');
            setNewProduct({ name: '', weight: 0, category: '', price: 0 });
            setShowAddProduct(false);
            loadVendorProducts();
        } catch (error) {
            console.error('Error adding product:', error);
            showError('Error adding product. Please try again.');
        }
    };

    const handleBulkAdd = async () => {
        if (!vendorId || !csvData.trim()) {
            showError('Please enter CSV data');
            return;
        }

        const lines = csvData.trim().split('\n');
        const newProducts = [];
        let errorLines = [];

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            if (!line) continue;

            const parts = line.split(',').map(part => part.trim());
            if (parts.length !== 4) {
                errorLines.push(`Line ${i + 1}: Expected 4 columns, got ${parts.length}`);
                continue;
            }

            const [name, category, weight, price] = parts;

            if (!predefinedCategories.includes(category)) {
                errorLines.push(`Line ${i + 1}: Invalid category "${category}". Must be one of: ${predefinedCategories.join(', ')}`);
                continue;
            }

            const weightNum = parseFloat(weight);
            const priceNum = parseFloat(price);

            if (isNaN(weightNum) || weightNum < 0) {
                errorLines.push(`Line ${i + 1}: Invalid weight "${weight}"`);
                continue;
            }

            if (isNaN(priceNum) || priceNum <= 0) {
                errorLines.push(`Line ${i + 1}: Invalid price "${price}"`);
                continue;
            }

            newProducts.push({
                name,
                category,
                weight: weightNum,
                price: priceNum
            });
        }

        if (errorLines.length > 0) {
            showError(`Found errors in CSV data:\n\n${errorLines.join('\n')}`);
            return;
        }

        if (newProducts.length === 0) {
            showError('No valid products found in CSV data');
            return;
        }

        try {
            for (const product of newProducts) {
                await deliveryApi.addProductToVendor(vendorId, product);
            }

            showSuccess(`Successfully added ${newProducts.length} products!`);
            setCsvData('');
            setShowAddProduct(false);
            setBulkMode(false);
            loadVendorProducts();
        } catch (error) {
            console.error('Error adding products:', error);
            showError('Error adding products. Please try again.');
        }
    };

    const handleEditProduct = (product: ProductData) => {
        setEditingProduct(product);
        setNewProduct({
            name: product.name,
            weight: product.weight,
            category: product.category,
            price: product.price
        });
        setShowAddProduct(true);
    }; const handleUpdateProduct = async () => {
        if (!editingProduct || !editingProduct.id || !newProduct.name || !newProduct.category || newProduct.price <= 0) {
            showError('Please fill in all required fields including category');
            return;
        }

        try {
            const productToUpdate = {
                ...newProduct,
                vendorId: editingProduct.vendorId || vendorId!
            };
            await deliveryApi.updateProduct(editingProduct.id, productToUpdate);
            showSuccess('Product updated successfully!');
            setNewProduct({ name: '', weight: 0, category: '', price: 0 });
            setEditingProduct(null);
            setShowAddProduct(false);
            loadVendorProducts();
        } catch (error) {
            console.error('Error updating product:', error);
            showError('Error updating product. Please try again.');
        }
    }; const handleDeleteProduct = async (productId: number) => {
        const confirmed = await showConfirmation({
            title: 'Delete Product',
            message: 'Are you sure you want to delete this product? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel',
            confirmColor: '#dc3545'
        });

        if (!confirmed) return;

        try {
            await deliveryApi.deleteProduct(productId);
            showSuccess('Product deleted successfully!');
            loadVendorProducts();
        } catch (error) {
            console.error('Error deleting product:', error);
            showError('Error deleting product. Please try again.');
        }
    };

    const handleCancelEdit = () => {
        setShowAddProduct(false);
        setEditingProduct(null);
        setBulkMode(false);
        setCsvData('');
        setNewProduct({ name: '', weight: 0, category: '', price: 0 });
    };

    if (!isOpen) return null;

    return (
        <div className="modal">
            <div className="modal-content">
                <div className="modal-header">
                    <h3>📦 Products - {vendorName}</h3>
                    <button className="close-button" onClick={onClose}>×</button>
                </div>

                {loading ? (
                    <div style={{ textAlign: 'center', padding: '20px' }}>
                        Loading products...
                    </div>
                ) : (
                    <div>
                        {products.length === 0 ? (
                            <p>No products available for this vendor.</p>
                        ) : (
                            <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                                {products.map(product => (
                                    <div key={product.id} className="product-item" style={{
                                        padding: '12px',
                                        marginBottom: '10px',
                                        border: '1px solid #ddd',
                                        borderRadius: '6px',
                                        backgroundColor: '#f9f9f9'
                                    }}>
                                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                                            <div style={{ flex: 1 }}>
                                                <h4 style={{ margin: '0 0 8px 0', color: '#333', fontSize: '16px' }}>
                                                    {product.name}
                                                </h4>
                                                <div style={{ fontSize: '14px', color: '#666' }}>
                                                    <span style={{
                                                        display: 'inline-block',
                                                        background: '#e9ecef',
                                                        padding: '2px 8px',
                                                        borderRadius: '12px',
                                                        marginRight: '8px',
                                                        fontSize: '12px'
                                                    }}>
                                                        {product.category}
                                                    </span>
                                                    <span>Weight: {product.weight} kg</span>
                                                </div>
                                            </div>
                                            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                                                <div style={{
                                                    fontSize: '18px',
                                                    fontWeight: 'bold',
                                                    color: '#28a745'
                                                }}>
                                                    ${product.price.toFixed(2)}
                                                </div>
                                                <div style={{ display: 'flex', gap: '5px' }}>
                                                    <button className='action-button'
                                                        onClick={() => handleEditProduct(product)}
                                                        style={{
                                                            border: 'none',
                                                            borderRadius: '4px',
                                                            padding: '4px 8px',
                                                            cursor: 'pointer',
                                                            fontSize: '14px'
                                                        }}
                                                        title="Edit product"
                                                    >
                                                        ✏️
                                                    </button>
                                                    <button className='action-button'
                                                        onClick={() => handleDeleteProduct(product.id || 0)}
                                                        style={{
                                                            border: 'none',
                                                            borderRadius: '4px',
                                                            padding: '4px 8px',
                                                            cursor: 'pointer',
                                                            fontSize: '14px'
                                                        }}
                                                        title="Delete product"
                                                    >
                                                        ❌
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}

                        <div className="form-actions" style={{ marginTop: '20px' }}>
                            <button className="btn-secondary" onClick={onClose}>
                                Close
                            </button>
                            <button
                                className="btn-primary"
                                onClick={() => {
                                    setBulkMode(false);
                                    setShowAddProduct(true);
                                }}
                                style={{ marginLeft: '10px' }}
                            >
                                📋 Add Single Product
                            </button>
                            <button
                                className="btn-primary btn-multiple"
                                onClick={() => {
                                    setBulkMode(true);
                                    setShowAddProduct(true);
                                }}
                                style={{ marginLeft: '10px', backgroundColor: '#17a2b8' }}
                            >
                                📝 Add Multiple Products (CSV)
                            </button>
                        </div>

                        {showAddProduct && (
                            <div style={{
                                marginTop: '20px',
                                padding: '15px',
                                border: '1px solid #ddd',
                                borderRadius: '6px',
                                backgroundColor: '#f8f9fa'
                            }}>
                                <h4 style={{ marginBottom: '15px' }}>
                                    {editingProduct ? 'Edit Product' : 'Add New Product'}
                                </h4>
                                {!bulkMode ? (
                                    <>
                                        <div style={{ marginBottom: '10px' }}>
                                            <label style={{ display: 'block', marginBottom: '5px' }}>Product Name:</label>
                                            <input
                                                type="text"
                                                value={newProduct.name}
                                                onChange={(e) => setNewProduct({ ...newProduct, name: e.target.value })}
                                                style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ddd' }}
                                                placeholder="Enter product name"
                                            />
                                        </div>

                                        <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
                                            <div style={{ flex: 1 }}>
                                                <label style={{ display: 'block', marginBottom: '5px' }}>Category:</label>
                                                <select
                                                    value={newProduct.category}
                                                    onChange={(e) => setNewProduct({ ...newProduct, category: e.target.value })}
                                                    style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ddd' }}
                                                >
                                                    <option value="">Select a category</option>
                                                    {predefinedCategories.map(category => (
                                                        <option key={category} value={category}>{category}</option>
                                                    ))}
                                                </select>
                                            </div>
                                            <div style={{ flex: 1 }}>
                                                <label style={{ display: 'block', marginBottom: '5px' }}>Weight (kg):</label>
                                                <input
                                                    type="number"
                                                    step="0.1"
                                                    value={newProduct.weight}
                                                    onChange={(e) => setNewProduct({ ...newProduct, weight: parseFloat(e.target.value) || 0 })}
                                                    style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ddd' }}
                                                />
                                            </div>
                                            <div style={{ flex: 1 }}>
                                                <label style={{ display: 'block', marginBottom: '5px' }}>Price ($):</label>
                                                <input
                                                    type="number"
                                                    step="0.01"
                                                    value={newProduct.price}
                                                    onChange={(e) => setNewProduct({ ...newProduct, price: parseFloat(e.target.value) || 0 })}
                                                    style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ddd' }}
                                                />
                                            </div>
                                        </div>

                                        <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end' }}>
                                            <button className='cancel-button'
                                                onClick={handleCancelEdit}
                                                style={{ padding: '8px 15px', backgroundColor: '#6c757d', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                                            >
                                                Cancel
                                            </button>
                                            <button className='save-button'
                                                onClick={editingProduct ? handleUpdateProduct : handleAddProduct}
                                                style={{ padding: '8px 15px', backgroundColor: '#28a745', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                                            >
                                                {editingProduct ? 'Update Product' : 'Save Product'}
                                            </button>
                                        </div>
                                    </>
                                ) : (
                                    <>
                                        <div style={{ marginBottom: '15px', padding: '10px', backgroundColor: '#e7f3ff', borderRadius: '5px', fontSize: '14px' }}>
                                            <strong>CSV Format:</strong> name,category,weight,price<br />
                                            <strong>Available categories:</strong> {predefinedCategories.join(', ')}<br />
                                            <strong>Example:</strong><br />
                                            Fresh Bread,Bakery,0.5,3.50<br />
                                            Whole Milk,Dairy,1.0,2.99<br />
                                            Chicken Breast,Meat,0.8,8.99
                                        </div>
                                        <div style={{ marginBottom: '10px' }}>
                                            <label style={{ display: 'block', marginBottom: '5px' }}>CSV Data:</label>
                                            <textarea
                                                value={csvData}
                                                onChange={(e) => setCsvData(e.target.value)}
                                                style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ddd', minHeight: '120px', fontFamily: 'monospace' }}
                                                placeholder="Enter CSV data (name,category,weight,price)"
                                            />
                                        </div>
                                        <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end' }}>
                                            <button className='cancel-button'
                                                onClick={handleCancelEdit}
                                                style={{ padding: '8px 15px', backgroundColor: '#6c757d', color: 'white', border: 'none', borderRadius: '4px' }}
                                            >
                                                Cancel
                                            </button>
                                            <button className='save-button'
                                                onClick={handleBulkAdd}
                                                style={{ padding: '8px 15px', backgroundColor: '#28a745', color: 'white', border: 'none', borderRadius: '4px' }}
                                            >
                                                Add Products
                                            </button>
                                        </div>
                                    </>
                                )}
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
};
