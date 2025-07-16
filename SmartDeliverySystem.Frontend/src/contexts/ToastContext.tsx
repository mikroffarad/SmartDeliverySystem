import React, { createContext, useContext, useCallback, useState } from 'react';
import { ToastMessage, ToastNotification } from '../components/ToastNotification';

interface ToastContextType {
    showToast: (message: string, type?: 'success' | 'error' | 'warning' | 'info', duration?: number) => void;
    showSuccess: (message: string, duration?: number) => void;
    showError: (message: string, duration?: number) => void;
    showWarning: (message: string, duration?: number) => void;
    showInfo: (message: string, duration?: number) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const useToast = () => {
    const context = useContext(ToastContext);
    if (!context) {
        throw new Error('useToast must be used within a ToastProvider');
    }
    return context;
};

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [toasts, setToasts] = useState<ToastMessage[]>([]);

    const showToast = useCallback((message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info', duration: number = 5000) => {
        const id = Date.now().toString() + Math.random().toString(36).substr(2, 9);
        const toast: ToastMessage = {
            id,
            message,
            type,
            duration
        };

        setToasts(prev => [...prev, toast]);
    }, []);

    const showSuccess = useCallback((message: string, duration: number = 5000) => {
        showToast(message, 'success', duration);
    }, [showToast]);

    const showError = useCallback((message: string, duration: number = 7000) => {
        showToast(message, 'error', duration);
    }, [showToast]);

    const showWarning = useCallback((message: string, duration: number = 6000) => {
        showToast(message, 'warning', duration);
    }, [showToast]);

    const showInfo = useCallback((message: string, duration: number = 5000) => {
        showToast(message, 'info', duration);
    }, [showToast]);

    const removeToast = useCallback((id: string) => {
        setToasts(prev => prev.filter(toast => toast.id !== id));
    }, []);

    const value = {
        showToast,
        showSuccess,
        showError,
        showWarning,
        showInfo
    };

    return (
        <ToastContext.Provider value={value}>
            {children}
            <ToastNotification toasts={toasts} onRemove={removeToast} />
        </ToastContext.Provider>
    );
};
