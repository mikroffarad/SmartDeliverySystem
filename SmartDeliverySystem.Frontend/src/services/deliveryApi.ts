import {
    DeliveryData,
    LocationData,
    VendorData,
    StoreData,
    InventoryItem,
    DeliveryRequest,
    PaymentData,
    DriverData,
    ProductData
} from '../types/delivery';

const API_BASE_URL = 'https://localhost:7183/api';

class DeliveryApi {

    // Delivery endpoints
    async getActiveDeliveries(): Promise<DeliveryData[]> {
        const response = await fetch(`${API_BASE_URL}/delivery/tracking/active`);
        if (!response.ok) throw new Error('Failed to fetch active deliveries');
        return response.json();
    }

    async getDeliveryById(deliveryId: number): Promise<DeliveryData> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}`);
        if (!response.ok) throw new Error('Failed to fetch delivery');
        return response.json();
    }

    async getAllDeliveries(): Promise<any[]> {
        const response = await fetch(`${API_BASE_URL}/delivery/all`);
        if (!response.ok) {
            throw new Error('Failed to fetch all deliveries');
        }
        return response.json();
    }

    async getDeliveryLocationHistory(deliveryId: number): Promise<any[]> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}/location-history`);
        if (!response.ok) {
            throw new Error('Failed to fetch delivery location history');
        }
        return response.json();
    }

    async getDeliveryProducts(deliveryId: number): Promise<any[]> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}/products`);
        if (!response.ok) {
            throw new Error('Failed to fetch delivery products');
        }
        return response.json();
    }

    async createDeliveryRequest(request: DeliveryRequest): Promise<{ deliveryId: number; totalAmount: number }> {
        const response = await fetch(`${API_BASE_URL}/delivery/request`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to create delivery request');
        }
        return response.json();
    }

    async processPayment(deliveryId: number, paymentData: PaymentData): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}/pay`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(paymentData)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to process payment');
        }
    }

    async assignDriver(deliveryId: number, driverData: DriverData): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}/assign-driver`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(driverData)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to assign driver');
        }
    }

    async updateDeliveryStatus(deliveryId: number, status: number): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}/status`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(status)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to update delivery status');
        }
    }

    async cancelDelivery(deliveryId: number): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}/status`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(5) // 5 = Cancelled enum value
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to cancel delivery');
        }
    }

    // Vendor endpoints
    async getAllVendors(): Promise<VendorData[]> {
        const response = await fetch(`${API_BASE_URL}/vendors/map`);
        if (!response.ok) throw new Error('Failed to fetch vendors');
        return response.json();
    }

    async createVendor(vendorData: LocationData): Promise<VendorData> {
        const response = await fetch(`${API_BASE_URL}/vendors`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(vendorData)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to create vendor');
        }
        return response.json();
    }

    async deleteVendor(vendorId: number): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/vendors/${vendorId}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to delete vendor');
        }
    }

    // Store endpoints
    async getAllStores(): Promise<StoreData[]> {
        const response = await fetch(`${API_BASE_URL}/stores/map`);
        if (!response.ok) throw new Error('Failed to fetch stores');
        return response.json();
    }

    async createStore(storeData: LocationData): Promise<StoreData> {
        const response = await fetch(`${API_BASE_URL}/stores`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(storeData)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to create store');
        }
        return response.json();
    }

    async deleteStore(storeId: number): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/stores/${storeId}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to delete store');
        }
    }

    async getStoreInventory(storeId: number): Promise<InventoryItem[]> {
        const response = await fetch(`${API_BASE_URL}/stores/${storeId}/inventory`);
        if (!response.ok) throw new Error('Failed to fetch store inventory');
        return response.json();
    }

    async findBestStore(vendorId: number, products: { productId: number; quantity: number }[]): Promise<{
        storeId: number;
        storeName: string;
        distance?: number;
    }> {
        const response = await fetch(`${API_BASE_URL}/delivery/find-best-store`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ vendorId, products })
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to find best store');
        }
        return response.json();
    }

    // Vendor products endpoints
    async getVendorProducts(vendorId: number): Promise<ProductData[]> {
        const response = await fetch(`${API_BASE_URL}/vendors/${vendorId}/products`);
        if (!response.ok) throw new Error('Failed to fetch vendor products');
        return response.json();
    }

    async addProductToVendor(vendorId: number, product: Omit<ProductData, 'id' | 'vendorId'>): Promise<ProductData> {
        const response = await fetch(`${API_BASE_URL}/vendors/${vendorId}/products`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(product)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to add product');
        }
        return response.json();
    }

    async updateProduct(productId: number, product: Omit<ProductData, 'id'> & { vendorId: number }): Promise<ProductData> {
        const response = await fetch(`${API_BASE_URL}/products/${productId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(product)
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to update product');
        }

        // If the response is empty (204 No Content), we return the product with its ID
        if (response.status === 204 || response.headers.get('content-length') === '0') {
            return { id: productId, ...product } as ProductData;
        }

        // Check if there is content to parse
        const text = await response.text();
        if (!text) {
            return { id: productId, ...product } as ProductData;
        }

        return JSON.parse(text);
    }

    async deleteProduct(productId: number): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/products/${productId}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to delete product');
        }
    }

    async deleteDelivery(deliveryId: number): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/delivery/${deliveryId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(error || 'Failed to delete delivery');
        }
    }
}

export const deliveryApi = new DeliveryApi();
