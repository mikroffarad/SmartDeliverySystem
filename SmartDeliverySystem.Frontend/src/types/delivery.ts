export interface LocationData {
    id?: number;
    name: string;
    latitude: number;
    longitude: number;
}

export interface ProductData {
    id?: number;
    name: string;
    weight: number;
    category: string;
    price: number;
    vendorId?: number;
}

export interface VendorData extends LocationData {
    products?: ProductData[];
}

export interface StoreData extends LocationData {
    inventory?: InventoryItem[];
}

export interface ProductData {
    id: number;
    name: string;
    weight: number;
    category: string;
    price: number;
    vendorId: number;
}

export interface InventoryItem {
    id?: number;
    productId: number;
    productName: string;
    category: string;
    quantity: number;
    price: number;
    storeId: number;
}

export interface DeliveryData {
    deliveryId: number;
    vendorId: number;
    storeId: number;
    status: DeliveryStatus;
    currentLatitude?: number;
    currentLongitude?: number;
    vendorLatitude?: number;
    vendorLongitude?: number;
    storeLatitude?: number;
    storeLongitude?: number;
    fromLatitude?: number;
    fromLongitude?: number;
    toLatitude?: number;
    toLongitude?: number;
    driverId?: string;
    gpsTrackerId?: string;
    lastLocationUpdate?: string;
    totalAmount?: number;
    products?: DeliveryProductData[];
}

export interface DeliveryProductData {
    productId: number;
    quantity: number;
    price?: number;
}

export interface DeliveryRequest {
    vendorId: number;
    storeId: number;
    products: DeliveryProductData[];
}

export interface PaymentData {
    amount: number;
    paymentMethod: 'CreditCard' | 'Cash' | 'BankTransfer';
}

export interface DriverData {
    driverId: string;
    gpsTrackerId: string;
}

export interface LocationUpdateData {
    deliveryId: number;
    latitude: number;
    longitude: number;
    timestamp: string;
    speed?: number;
    notes?: string;
}

export type DeliveryStatus =
    | 'Pending'
    | 'PendingPayment'
    | 'Assigned'
    | 'InTransit'
    | 'Delivered'
    | 'Paid'
    | 'Cancelled';

export type ConnectionStatus = 'connected' | 'disconnected' | 'error';
