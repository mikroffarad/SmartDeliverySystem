// Utility for converting delivery statuses
export const getStatusText = (status: number | string): string => {
    if (typeof status === 'string') return status;

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

// Utility for getting status color
export const getStatusColor = (status: number | string): string => {
    const statusText = getStatusText(status);

    switch (statusText) {
        case 'PendingPayment': return '#ffc107';
        case 'Paid': return '#17a2b8';
        case 'Assigned': return '#fd7e14';
        case 'InTransit': return '#007bff';
        case 'Delivered': return '#28a745';
        case 'Cancelled': return '#dc3545';
        default: return '#6c757d';
    }
};
