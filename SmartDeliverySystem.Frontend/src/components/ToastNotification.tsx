import React, { useEffect, useState } from 'react';

export interface ToastMessage {
    id: string;
    message: string;
    type: 'success' | 'error' | 'warning' | 'info';
    duration?: number;
}

interface ToastNotificationProps {
    toasts: ToastMessage[];
    onRemove: (id: string) => void;
}

export const ToastNotification: React.FC<ToastNotificationProps> = ({ toasts, onRemove }) => {
    return (
        <div style={{
            position: 'fixed',
            top: '20px',
            left: '50%',
            transform: 'translateX(-50%)',
            zIndex: 9999,
            display: 'flex',
            flexDirection: 'column-reverse',
            gap: '10px',
            maxWidth: '500px',
            width: '90%'
        }}>
            {toasts.map(toast => (
                <ToastItem key={toast.id} toast={toast} onRemove={onRemove} />
            ))}
        </div>
    );
};

const ToastItem: React.FC<{ toast: ToastMessage; onRemove: (id: string) => void }> = ({ toast, onRemove }) => {
    const [isVisible, setIsVisible] = useState(false);

    useEffect(() => {
        // Animate in
        const timer = setTimeout(() => setIsVisible(true), 10);

        // Auto-hide after duration
        const hideTimer = setTimeout(() => {
            setIsVisible(false);
            setTimeout(() => onRemove(toast.id), 300); // Wait for animation
        }, toast.duration || 5000);

        return () => {
            clearTimeout(timer);
            clearTimeout(hideTimer);
        };
    }, [toast.id, toast.duration, onRemove]); const getToastStyles = () => {
        const baseStyles = {
            padding: '12px 16px',
            borderRadius: '8px',
            color: 'white',
            fontSize: '14px',
            fontWeight: '500',
            boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            cursor: 'pointer',
            transition: 'all 0.3s ease',
            transform: isVisible ? 'translateY(0)' : 'translateY(-100%)',
            opacity: isVisible ? 1 : 0,
            marginBottom: '8px'
        };

        switch (toast.type) {
            case 'success':
                return { ...baseStyles, backgroundColor: '#28a745' };
            case 'error':
                return { ...baseStyles, backgroundColor: '#dc3545' };
            case 'warning':
                return { ...baseStyles, backgroundColor: '#ffc107', color: '#212529' };
            case 'info':
                return { ...baseStyles, backgroundColor: '#17a2b8' };
            default:
                return { ...baseStyles, backgroundColor: '#6c757d' };
        }
    };

    const getIcon = () => {
        switch (toast.type) {
            case 'success':
                return '✅';
            case 'error':
                return '❌';
            case 'warning':
                return '⚠️';
            case 'info':
                return 'ℹ️';
            default:
                return '📢';
        }
    };

    return (
        <div style={getToastStyles()} onClick={() => onRemove(toast.id)}>
            <span style={{ fontSize: '16px' }}>{getIcon()}</span>
            <span style={{ flex: 1 }}>{toast.message}</span>
            <span style={{ fontSize: '12px', opacity: 0.8 }}>✕</span>
        </div>
    );
};
